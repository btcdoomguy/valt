using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat;

internal class FiatHistoryUpdaterJob : IBackgroundJob
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly IFiatHistoricalDataProvider _provider;
    private readonly ConfigurationManager _configurationManager;
    private readonly ILogger<FiatHistoryUpdaterJob> _logger;

    public string Name => "Fiat history updater job";

    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.FiatHistoryUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;

    public TimeSpan Interval => TimeSpan.FromSeconds(120);


    public FiatHistoryUpdaterJob(IPriceDatabase priceDatabase,
        IFiatHistoricalDataProvider provider,
        ConfigurationManager configurationManager,
        ILogger<FiatHistoryUpdaterJob> logger)
    {
        _priceDatabase = priceDatabase;
        _provider = provider;
        _configurationManager = configurationManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[FiatHistoryUpdater] Starting...");
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[FiatHistoryUpdater] Updating fiat history");
        try
        {
            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);

            if (!_priceDatabase.HasDatabaseOpen)
                return;

            // Check if price database has any fiat data
            var hasFiatData = false;
            var historicalLastDate = new DateTime(2008, 1, 1);
            try
            {
                historicalLastDate = _priceDatabase.GetFiatData().FindAll().Max(x => x.Date);
                hasFiatData = true;
            }
            catch (InvalidOperationException)
            {
                //ignore - no data available
            }
            catch (NotSupportedException)
            {
                //ignore - no data available
            }

            // If price database is empty and no local database is open, skip
            if (!hasFiatData && !_configurationManager.HasLocalDatabaseOpen)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Price database is empty and no local database open, skipping update");
                return;
            }

            var startDate = historicalLastDate.AddDays(1);
            var endDate = localDate.AddDays(-1);

            //skip useless calls
            while (startDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                startDate = startDate.AddDays(1);

            while (endDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                endDate = endDate.AddDays(-1);

            if (startDate > endDate)
                return;

            _logger.LogInformation("[FiatHistoryUpdater] From {0} to {1}", startDate!.ToShortDateString(),
                endDate.ToShortDateString());

            // Get currencies to fetch:
            // - If local database is open, use configuration
            // - If not, use currencies already in price database
            var currencies = GetCurrenciesToFetch();
            if (currencies.Count == 0)
            {
                _logger.LogInformation("[FiatHistoryUpdater] No currencies to fetch, skipping update");
                return;
            }

            var prices = (await _provider.GetPricesAsync(DateOnly.FromDateTime(startDate),
                DateOnly.FromDateTime(endDate), currencies).ConfigureAwait(false)).ToList();

            if (prices.Count != 0)
            {
                FillLocalDatabase(prices);
                WeakReferenceMessenger.Default.Send<FiatHistoryPriceUpdatedMessage>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FiatHistoryUpdater] Error during execution");
            throw;
        }
    }

    /// <summary>
    /// Gets the list of currencies to fetch for historical data.
    /// If local database is open, uses configuration.
    /// If configuration is empty, falls back to price database currencies.
    /// </summary>
    private List<string> GetCurrenciesToFetch()
    {
        if (_configurationManager.HasLocalDatabaseOpen)
        {
            var configCurrencies = _configurationManager.GetAvailableFiatCurrencies();
            if (configCurrencies.Count > 0)
            {
                return configCurrencies;
            }
            // Fall through to use price database currencies if config is empty
        }

        // Extract currencies from existing price database data
        try
        {
            var currencies = _priceDatabase.GetFiatData()
                .FindAll()
                .Select(x => x.Currency)
                .Distinct()
                .ToList();

            _logger.LogInformation("[FiatHistoryUpdater] Using {Count} currencies from price database", currencies.Count);
            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FiatHistoryUpdater] Error getting currencies from price database");
            return new List<string>();
        }
    }

    private void FillLocalDatabase(IEnumerable<IFiatHistoricalDataProvider.FiatPriceData> prices)
    {
        var pricesList = prices.ToList();
        if (pricesList.Count == 0)
            return;

        var minDate = pricesList.Min(p => p.Date).ToValtDateTime();
        var maxDate = pricesList.Max(p => p.Date).ToValtDateTime();

        var existingEntries = _priceDatabase.GetFiatData()
            .Find(x => x.Date >= minDate && x.Date <= maxDate)
            .Select(x => (x.Date, x.Currency))
            .ToHashSet();

        var entries = new List<FiatDataEntity>();
        foreach (var price in pricesList)
        {
            var dateToConsider = price.Date.ToValtDateTime();
            foreach (var currency in price.Data)
            {
                if (existingEntries.Contains((dateToConsider, currency.Currency)))
                {
                    _logger.LogDebug(
                        "[FiatHistoryUpdater] Skipping duplicate entry for {Date} {Currency}",
                        dateToConsider.ToString("yyyy-MM-dd"), currency.Currency);
                    continue;
                }

                _logger.LogInformation(
                    "[FiatHistoryUpdater] Adding price {CurrencyPrice} for {S} for {CurrencyCurrency}",
                    currency.Price, dateToConsider.ToString("yyyy-MM-dd"), currency.Currency);
                entries.Add(new FiatDataEntity()
                {
                    Date = dateToConsider,
                    Currency = currency.Currency,
                    Price = currency.Price
                });
            }
        }

        if (entries.Count > 0)
            _priceDatabase.GetFiatData().Insert(entries);
    }
}