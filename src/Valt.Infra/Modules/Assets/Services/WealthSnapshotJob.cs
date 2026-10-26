using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.App.Modules.Assets.Contracts;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Goals.Services;

namespace Valt.Infra.Modules.Assets.Services;

/// <summary>
/// Writes one wealth snapshot per local day into the "wealth_snapshots" collection.
/// The snapshotted value is <see cref="IAssetQueries.GetSummaryAsync"/> (assets flagged
/// IncludeInNetWorth and not sold) computed with the latest stored rates — the same
/// single semantic source that feeds the Transactions tab "Assets Total" bar.
/// A second write on the same day overwrites the existing row (locked decision D-03).
/// </summary>
internal sealed class WealthSnapshotJob : IBackgroundJob
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IWealthSnapshotRepository _wealthSnapshotRepository;
    private readonly IAssetQueries _assetQueries;
    private readonly IPriceDatabaseRatesProvider _ratesProvider;
    private readonly IClock _clock;
    private readonly ILogger<WealthSnapshotJob> _logger;

    public string Name => "Wealth Snapshot";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.WealthSnapshot;
    public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
    public TimeSpan Interval => TimeSpan.FromMinutes(5);

    public WealthSnapshotJob(
        ILocalDatabase localDatabase,
        IWealthSnapshotRepository wealthSnapshotRepository,
        IAssetQueries assetQueries,
        IPriceDatabaseRatesProvider ratesProvider,
        IClock clock,
        ILogger<WealthSnapshotJob> logger)
    {
        _localDatabase = localDatabase;
        _wealthSnapshotRepository = wealthSnapshotRepository;
        _assetQueries = assetQueries;
        _ratesProvider = ratesProvider;
        _clock = clock;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[WealthSnapshotJob] Started");
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        if (!_localDatabase.HasDatabaseOpen)
        {
            _logger.LogDebug("[WealthSnapshotJob] Local database not open, skipping");
            return;
        }

        var mainCurrencyCode = GoalContributingTransactionsCurrency.GetMainFiatCurrencyCode(_localDatabase);

        var rates = await _ratesProvider.GetLatestRatesAsync(stoppingToken);
        var btcPriceUsd = rates?.Btc.Items.FirstOrDefault(x => x.CurrencyCode == FiatCurrency.Usd.Code)?.Price;
        IReadOnlyDictionary<string, decimal>? fiatRates = rates?.Fiat.Items
            .ToDictionary(x => x.Currency.Code, x => x.Price);

        if (rates is null || btcPriceUsd is null || fiatRates is null)
        {
            // GetSummaryAsync returns all-zero totals when rates are missing (AssetQueries.cs:110-111);
            // a zero row would be joined as "latest ≤ period end" forever — never write it.
            _logger.LogDebug("[WealthSnapshotJob] Rates unavailable, skipping snapshot write");
            return;
        }

        var summary = await _assetQueries.GetSummaryAsync(mainCurrencyCode, btcPriceUsd, fiatRates);
        var today = _clock.GetCurrentLocalDate();

        var existing = await _wealthSnapshotRepository.GetByDayAsync(today);
        if (existing is not null &&
            existing.TotalValueInMainCurrency == summary.TotalValueInMainCurrency &&
            existing.TotalValueInSats == summary.TotalValueInSats &&
            existing.MainCurrencyCode == mainCurrencyCode)
        {
            _logger.LogDebug("[WealthSnapshotJob] Snapshot unchanged for {Day}, skipping write",
                today.ToString("yyyy-MM-dd"));
            return;
        }

        var snapshot = new WealthSnapshotEntity
        {
            Id = ObjectId.NewObjectId(),
            SnapshotDateTimeUtc = _clock.GetCurrentDateTimeUtc(),
            SnapshotDay = today,
            TotalValueInMainCurrency = summary.TotalValueInMainCurrency,
            TotalValueInSats = summary.TotalValueInSats,
            MainCurrencyCode = mainCurrencyCode
        };

        await _wealthSnapshotRepository.SaveAsync(snapshot);

        _logger.LogDebug(
            "[WealthSnapshotJob] Snapshot saved for {Day}: {Fiat} {Currency} / {Sats} sats",
            snapshot.SnapshotDay.ToString("yyyy-MM-dd"),
            snapshot.TotalValueInMainCurrency,
            snapshot.MainCurrencyCode,
            snapshot.TotalValueInSats);
    }
}
