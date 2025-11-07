using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.DataSources.Bitcoin;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;

internal class BitcoinHistoryUpdaterJob : IBackgroundJob
{
    private readonly IBitcoinHistoricalDataProvider _provider;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILogger<BitcoinHistoryUpdaterJob> _logger;

    public string Name => "Bitcoin history updater job";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.BitcoinHistoryUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;
    
    public TimeSpan Interval => TimeSpan.FromSeconds(120);

    public BitcoinHistoryUpdaterJob(IBitcoinHistoricalDataProvider provider,
        IPriceDatabase priceDatabase,
        ILogger<BitcoinHistoryUpdaterJob> logger)
    {
        _provider = provider;
        _priceDatabase = priceDatabase;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BitcoinHistoryUpdaterJob] Started");
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BitcoinHistoryUpdaterJob] Updating BTC history");

        try
        {
            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);

            if (!_priceDatabase.HasDatabaseOpen)
                return;
            
            var lastStoredDate = _priceDatabase.GetBitcoinData().Max(x => x.Date).Date;
            var endDate = localDate.Date.AddDays(-1);

            if (lastStoredDate >= endDate)
                return;

            _logger.LogInformation("[BitcoinHistoryUpdaterJob] From {0} to {1}", lastStoredDate.ToShortDateString(),
                endDate.ToShortDateString());

            var prices = await _provider.GetPricesAsync(DateOnly.FromDateTime(lastStoredDate),
                DateOnly.FromDateTime(endDate));

            var entries = new List<BitcoinDataEntity>();
            foreach (var price in prices)
            {
                var dateToConsider = price.Date.ToValtDateTime();
                if (dateToConsider <= endDate)
                {
                    _logger.LogInformation("[BitcoinHistoryUpdaterJob] Adding price {PricePrice} for {DateToConsider}",
                        price.Price, dateToConsider.ToString("yyyy-MM-dd"));
                    
                    entries.Add(new BitcoinDataEntity()
                    {
                        Date = dateToConsider,
                        Price = price.Price
                    });
                }
            }
            
            if (entries.Count != 0)
            {
                _priceDatabase.GetBitcoinData().Insert(entries);
                WeakReferenceMessenger.Default.Send<BitcoinHistoryPriceUpdatedMessage>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BitcoinHistoryUpdaterJob] Error during execution");
            throw;
        }
    }
}