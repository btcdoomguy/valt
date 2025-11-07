using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat;

internal class FiatHistoryUpdaterJob : IBackgroundJob
{
    private readonly IFiatHistoricalDataProvider _provider;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILogger<FiatHistoryUpdaterJob> _logger;

    public string Name => "Fiat history updater job";

    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.FiatHistoryUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;

    public TimeSpan Interval => TimeSpan.FromSeconds(120);

    public FiatHistoryUpdaterJob(IFiatHistoricalDataProvider provider,
        IPriceDatabase priceDatabase,
        ILogger<FiatHistoryUpdaterJob> logger)
    {
        _provider = provider;
        _priceDatabase = priceDatabase;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[FiatHistoryUpdaterJob] Started");
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[FiatHistoryUpdaterJob] Updating fiat history");
        try
        {
            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);

            if (!_priceDatabase.HasDatabaseOpen)
                return;


            var historicalLastDate = new DateTime(2008, 1, 1);
            try
            {
                historicalLastDate = _priceDatabase.GetFiatData().FindAll().Max(x => x.Date);
            }
            catch (InvalidOperationException)
            {
                //ignore - no data available
            }
            catch (NotSupportedException)
            {
                //ignore - no data available
            }

            var startDate = historicalLastDate;
            var endDate = localDate.AddDays(-1);

            if (startDate >= endDate)
                return;

            _logger.LogInformation("[FiatHistoryUpdaterJob] From {0} to {1}", startDate!.ToShortDateString(),
                endDate.ToShortDateString());

            var prices = (await _provider.GetPricesAsync(DateOnly.FromDateTime(startDate),
                DateOnly.FromDateTime(endDate))).ToList();

            if (prices.Count != 0)
            {
                FillLocalDatabase(prices);
                WeakReferenceMessenger.Default.Send<FiatHistoryPriceUpdatedMessage>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FiatHistoryUpdaterJob] Error during execution");
            throw;
        }
    }

    private void FillLocalDatabase(IEnumerable<IFiatHistoricalDataProvider.FiatPriceData> prices)
    {
        var entries = new List<FiatDataEntity>();
        foreach (var price in prices)
        {
            var dateToConsider = price.Date.ToValtDateTime();
            foreach (var currency in price.Data)
            {
                _logger.LogInformation(
                    "[FiatHistoryUpdaterJob] Adding price {CurrencyPrice} for {S} for {CurrencyCurrency}",
                    currency.Price, dateToConsider.ToString("yyyy-MM-dd"), currency.Currency);
                entries.Add(new FiatDataEntity()
                {
                    Date = dateToConsider,
                    Currency = currency.Currency,
                    Price = currency.Price
                });
            }
        }

        _priceDatabase.GetFiatData().Insert(entries);
    }
}