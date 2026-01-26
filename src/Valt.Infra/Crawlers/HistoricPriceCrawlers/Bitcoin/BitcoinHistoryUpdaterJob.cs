using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.DataSources.Bitcoin;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;

internal class BitcoinHistoryUpdaterJob : IBackgroundJob
{
    private readonly IBitcoinHistoricalDataProvider _provider;
    private readonly IBitcoinInitialSeedPriceProvider _seedProvider;
    private readonly IPriceDatabase _priceDatabase;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<BitcoinHistoryUpdaterJob> _logger;

    public string Name => "Bitcoin history updater job";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.BitcoinHistoryUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;

    public TimeSpan Interval => TimeSpan.FromSeconds(120);

    public BitcoinHistoryUpdaterJob(IBitcoinHistoricalDataProvider provider,
        IBitcoinInitialSeedPriceProvider seedProvider,
        IPriceDatabase priceDatabase,
        INotificationPublisher notificationPublisher,
        ILogger<BitcoinHistoryUpdaterJob> logger)
    {
        _provider = provider;
        _seedProvider = seedProvider;
        _priceDatabase = priceDatabase;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BitcoinHistoryUpdater] Starting...");
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BitcoinHistoryUpdater] Starting BTC history update cycle");

        if (!_priceDatabase.HasDatabaseOpen)
        {
            _logger.LogInformation("[BitcoinHistoryUpdater] Price database not open, skipping");
            return;
        }

        var recordCount = _priceDatabase.GetBitcoinData().Query().Count();
        _logger.LogInformation("[BitcoinHistoryUpdater] Current BTC price records: {Count}", recordCount);

        if (recordCount == 0)
        {
            _logger.LogInformation("[BitcoinHistoryUpdater] Starting the prices db with the initial seed");
            var seedPrices = await _seedProvider.GetPricesAsync();
            var seedList = seedPrices.ToList();
            _logger.LogInformation("[BitcoinHistoryUpdater] Loading {Count} seed prices", seedList.Count);

            _priceDatabase.GetBitcoinData().InsertBulk(seedList.Select(x => new BitcoinDataEntity()
            {
                Date = x.Date.ToValtDateTime(),
                Price = x.Price
            }));

            _priceDatabase.Checkpoint();
            _logger.LogInformation("[BitcoinHistoryUpdater] Seed data loaded successfully");
        }

        try
        {
            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);

            var lastStoredDate = _priceDatabase.GetBitcoinData().Max(x => x.Date).Date;
            var endDate = localDate.Date.AddDays(-1);

            _logger.LogInformation("[BitcoinHistoryUpdater] Last stored date: {LastDate}, Target end date: {EndDate}",
                lastStoredDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            if (lastStoredDate >= endDate)
            {
                _logger.LogInformation("[BitcoinHistoryUpdater] Already up to date, no new data needed");
                return;
            }

            var daysToFetch = (endDate - lastStoredDate).Days;
            _logger.LogInformation("[BitcoinHistoryUpdater] Fetching {Days} days of data from {Start} to {End}",
                daysToFetch, lastStoredDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var prices = await _provider.GetPricesAsync(DateOnly.FromDateTime(lastStoredDate),
                DateOnly.FromDateTime(endDate)).ConfigureAwait(false);

            var entries = new List<BitcoinDataEntity>();
            var endDateOnly = DateOnly.FromDateTime(endDate);
            foreach (var price in prices)
            {
                if (price.Date <= endDateOnly)
                {
                    var dateToStore = price.Date.ToValtDateTime();
                    _logger.LogInformation("[BitcoinHistoryUpdater] Adding BTC price ${Price:N2} for {Date}",
                        price.Price, dateToStore.ToString("yyyy-MM-dd"));

                    entries.Add(new BitcoinDataEntity()
                    {
                        Date = dateToStore,
                        Price = price.Price
                    });
                }
            }

            if (entries.Count != 0)
            {
                _priceDatabase.GetBitcoinData().Insert(entries);
                _priceDatabase.Checkpoint();
                _logger.LogInformation("[BitcoinHistoryUpdater] Inserted {Count} new BTC price records", entries.Count);
                await _notificationPublisher.PublishAsync(new BitcoinHistoryPriceUpdatedMessage());
            }
            else
            {
                _logger.LogInformation("[BitcoinHistoryUpdater] No new entries to insert");
            }

            _logger.LogInformation("[BitcoinHistoryUpdater] Update cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BitcoinHistoryUpdater] Error during execution");
            throw;
        }
    }
}