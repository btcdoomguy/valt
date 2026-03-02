using Microsoft.Extensions.Logging;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Crawlers.Indicators;

internal class IndicatorsUpdaterJob : IBackgroundJob
{
    private readonly IBitcoinComIndicatorsProvider _bitcoinComProvider;
    private readonly IFearAndGreedProvider _fearAndGreedProvider;
    private readonly IBitcoinDominanceProvider _dominanceProvider;
    private readonly IIndicatorCache _indicatorCache;
    private readonly IPriceDatabase _priceDatabase;
    private readonly IClock _clock;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<IndicatorsUpdaterJob> _logger;

    public string Name => "Indicators updater job";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.IndicatorsUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;
    public TimeSpan Interval => TimeSpan.FromSeconds(120);

    public IndicatorsUpdaterJob(
        IBitcoinComIndicatorsProvider bitcoinComProvider,
        IFearAndGreedProvider fearAndGreedProvider,
        IBitcoinDominanceProvider dominanceProvider,
        IIndicatorCache indicatorCache,
        IPriceDatabase priceDatabase,
        IClock clock,
        INotificationPublisher notificationPublisher,
        ILogger<IndicatorsUpdaterJob> logger)
    {
        _bitcoinComProvider = bitcoinComProvider;
        _fearAndGreedProvider = fearAndGreedProvider;
        _dominanceProvider = dominanceProvider;
        _indicatorCache = indicatorCache;
        _priceDatabase = priceDatabase;
        _clock = clock;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[IndicatorsUpdaterJob] Started");

        // Load cached snapshot on startup so UI shows stale data immediately
        var cached = _indicatorCache.GetLatest();
        if (cached is not null)
        {
            _logger.LogInformation("[IndicatorsUpdaterJob] Publishing cached indicator data from {Date}", cached.LastUpdatedUtc);
            await _notificationPublisher.PublishAsync(new IndicatorsUpdatedMessage(cached));
        }
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[IndicatorsUpdaterJob] Starting indicator update cycle");

        if (!_priceDatabase.HasDatabaseOpen)
        {
            _logger.LogInformation("[IndicatorsUpdaterJob] Price database not open, skipping update");
            return;
        }

        // Get previously cached values as fallback for any failed providers
        var previousSnapshot = _indicatorCache.GetLatest();

        // Fetch all indicators in parallel - each wrapped in try/catch
        var mayerTask = SafeFetchAsync(() => _bitcoinComProvider.GetMayerMultipleAsync(), "Mayer Multiple");
        var rainbowTask = SafeFetchAsync(() => _bitcoinComProvider.GetRainbowChartAsync(), "Rainbow Chart");
        var fearGreedTask = SafeFetchAsync(() => _fearAndGreedProvider.GetAsync(), "Fear & Greed");
        var dominanceTask = SafeFetchAsync(() => _dominanceProvider.GetAsync(), "BTC Dominance");

        await Task.WhenAll(mayerTask, rainbowTask, fearGreedTask, dominanceTask);

        var snapshot = new IndicatorSnapshot
        {
            LastUpdatedUtc = _clock.GetCurrentDateTimeUtc(),
            IsUpToDate = true,
            MayerMultiple = await mayerTask ?? previousSnapshot?.MayerMultiple,
            RainbowChart = await rainbowTask ?? previousSnapshot?.RainbowChart,
            FearAndGreed = await fearGreedTask ?? previousSnapshot?.FearAndGreed,
            BitcoinDominance = await dominanceTask ?? previousSnapshot?.BitcoinDominance
        };

        _indicatorCache.Save(snapshot);
        await _notificationPublisher.PublishAsync(new IndicatorsUpdatedMessage(snapshot));

        _logger.LogInformation("[IndicatorsUpdaterJob] Indicator update completed successfully");
    }

    private async Task<T?> SafeFetchAsync<T>(Func<Task<T>> fetcher, string indicatorName) where T : class
    {
        try
        {
            return await fetcher();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[IndicatorsUpdaterJob] Failed to fetch {Indicator}", indicatorName);
            return null;
        }
    }
}
