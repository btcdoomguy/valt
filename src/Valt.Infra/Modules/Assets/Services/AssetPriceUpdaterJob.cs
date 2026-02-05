using Microsoft.Extensions.Logging;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.Assets.PriceProviders;

namespace Valt.Infra.Modules.Assets.Services;

public record AssetPricesUpdated() : INotification;

internal sealed class AssetPriceUpdaterJob : IBackgroundJob
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DelayBetweenRequests = TimeSpan.FromSeconds(1);

    private readonly ILocalDatabase _localDatabase;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetPriceProviderSelector _priceProviderSelector;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<AssetPriceUpdaterJob> _logger;

    public string Name => "Asset Price Updater";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.AssetPriceUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
    public TimeSpan Interval => TimeSpan.FromMinutes(5);

    public AssetPriceUpdaterJob(
        ILocalDatabase localDatabase,
        IAssetRepository assetRepository,
        IAssetPriceProviderSelector priceProviderSelector,
        INotificationPublisher notificationPublisher,
        ILogger<AssetPriceUpdaterJob> logger)
    {
        _localDatabase = localDatabase;
        _assetRepository = assetRepository;
        _priceProviderSelector = priceProviderSelector;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AssetPriceUpdaterJob] Started");
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        if (!_localDatabase.HasDatabaseOpen)
        {
            _logger.LogDebug("[AssetPriceUpdaterJob] Local database not open, skipping");
            return;
        }

        try
        {
            var allAssets = await _assetRepository.GetAllAsync();
            var assetsToUpdate = allAssets
                .Where(ShouldUpdatePrice)
                .ToList();

            if (assetsToUpdate.Count == 0)
            {
                _logger.LogDebug("[AssetPriceUpdaterJob] No assets to update");
                return;
            }

            _logger.LogInformation("[AssetPriceUpdaterJob] Found {Count} assets to update", assetsToUpdate.Count);

            var updatedCount = 0;
            foreach (var asset in assetsToUpdate)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var (priceSource, symbol, currencyCode) = GetPriceInfo(asset);
                if (priceSource == AssetPriceSource.Manual || string.IsNullOrWhiteSpace(symbol))
                    continue;

                try
                {
                    var result = await _priceProviderSelector.GetPriceAsync(priceSource, symbol, currencyCode);
                    if (result is not null)
                    {
                        asset.UpdatePrice(result.Price);
                        await _assetRepository.SaveAsync(asset);
                        updatedCount++;

                        _logger.LogInformation(
                            "[AssetPriceUpdaterJob] Updated {AssetName} ({Symbol}): {Price} {Currency}",
                            asset.Name.Value, symbol, result.Price, result.CurrencyCode);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[AssetPriceUpdaterJob] Failed to get price for {AssetName} ({Symbol})",
                            asset.Name.Value, symbol);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[AssetPriceUpdaterJob] Error updating price for {AssetName} ({Symbol})",
                        asset.Name.Value, symbol);
                }

                // Add delay between requests to avoid rate limiting
                await Task.Delay(DelayBetweenRequests, stoppingToken);
            }

            if (updatedCount > 0)
            {
                _logger.LogInformation("[AssetPriceUpdaterJob] Successfully updated {Count} assets", updatedCount);
                await _notificationPublisher.PublishAsync(new AssetPricesUpdated());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AssetPriceUpdaterJob] Error during execution");
        }
    }

    private static bool ShouldUpdatePrice(Asset asset)
    {
        var (priceSource, symbol, _) = GetPriceInfo(asset);

        // Skip manual price sources
        if (priceSource == AssetPriceSource.Manual)
            return false;

        // Skip assets without symbols
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        // Check if price is stale (older than threshold)
        var timeSinceLastUpdate = DateTime.UtcNow - asset.LastPriceUpdateAt;
        return timeSinceLastUpdate > StaleThreshold;
    }

    private static (AssetPriceSource PriceSource, string? Symbol, string CurrencyCode) GetPriceInfo(Asset asset)
    {
        return asset.Details switch
        {
            BasicAssetDetails basic => (basic.PriceSource, basic.Symbol, basic.CurrencyCode),
            LeveragedPositionDetails leveraged => (leveraged.PriceSource, leveraged.Symbol, leveraged.CurrencyCode),
            _ => (AssetPriceSource.Manual, null, "USD")
        };
    }
}
