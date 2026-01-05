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
    private static readonly DateTime DefaultStartDate = new(2020, 1, 1);

    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalDatabase _localDatabase;
    private readonly IFiatHistoricalDataProvider _provider;
    private readonly ConfigurationManager _configurationManager;
    private readonly ILogger<FiatHistoryUpdaterJob> _logger;

    public string Name => "Fiat history updater job";

    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.FiatHistoryUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;

    public TimeSpan Interval => TimeSpan.FromSeconds(120);


    public FiatHistoryUpdaterJob(IPriceDatabase priceDatabase,
        ILocalDatabase localDatabase,
        IFiatHistoricalDataProvider provider,
        ConfigurationManager configurationManager,
        ILogger<FiatHistoryUpdaterJob> logger)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
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
        _logger.LogInformation("[FiatHistoryUpdater] Starting fiat history update cycle");
        try
        {
            // Skip if no local database is open (we need it for currency configuration)
            if (!_configurationManager.HasLocalDatabaseOpen)
            {
                _logger.LogInformation("[FiatHistoryUpdater] No local database open, skipping update");
                return;
            }

            if (!_priceDatabase.HasDatabaseOpen)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Price database not open, skipping update");
                return;
            }

            var currentRecordCount = _priceDatabase.GetFiatData().Query().Count();
            _logger.LogInformation("[FiatHistoryUpdater] Current fiat price records: {Count}", currentRecordCount);

            // Get currencies from configuration
            var currencies = _configurationManager.GetAvailableFiatCurrencies();
            if (currencies.Count == 0)
            {
                _logger.LogInformation("[FiatHistoryUpdater] No currencies configured, skipping update");
                return;
            }

            _logger.LogInformation("[FiatHistoryUpdater] Configured currencies: {Currencies}",
                string.Join(", ", currencies));

            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);
            var endDate = localDate.AddDays(-1);

            // Skip weekends for end date
            while (endDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                endDate = endDate.AddDays(-1);

            // Get fiat data date range from prices.db
            var (minFiatDate, maxFiatDate) = GetFiatDateRange();

            if (minFiatDate.HasValue && maxFiatDate.HasValue)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Current fiat date range: {MinDate} to {MaxDate}",
                    minFiatDate.Value.ToString("yyyy-MM-dd"), maxFiatDate.Value.ToString("yyyy-MM-dd"));
            }
            else
            {
                _logger.LogInformation("[FiatHistoryUpdater] No existing fiat data found");
            }

            // Calculate the required start date based on the lowest transaction date
            var requiredStartDate = GetRequiredStartDate();
            _logger.LogInformation("[FiatHistoryUpdater] Required start date: {StartDate}", requiredStartDate.ToString("yyyy-MM-dd"));

            var updated = false;

            // First: Fill backward gaps if needed (transaction date earlier than min fiat date)
            if (minFiatDate is not null && requiredStartDate < minFiatDate.Value)
            {
                var gapEndDate = minFiatDate.Value.AddDays(-1);
                while (gapEndDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    gapEndDate = gapEndDate.AddDays(-1);

                if (requiredStartDate <= gapEndDate)
                {
                    _logger.LogInformation("[FiatHistoryUpdater] Filling gap from {0} to {1}",
                        requiredStartDate.ToShortDateString(), gapEndDate.ToShortDateString());

                    var gapPrices = (await _provider.GetPricesAsync(
                        DateOnly.FromDateTime(requiredStartDate),
                        DateOnly.FromDateTime(gapEndDate),
                        currencies).ConfigureAwait(false)).ToList();

                    if (gapPrices.Count != 0)
                    {
                        FillLocalDatabase(gapPrices);
                        updated = true;
                    }
                }
            }

            // Second: Fill forward (new data after max fiat date)
            var startDate = maxFiatDate?.AddDays(1) ?? requiredStartDate;

            // Skip weekends for start date
            while (startDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                startDate = startDate.AddDays(1);

            if (startDate <= endDate)
            {
                _logger.LogInformation("[FiatHistoryUpdater] From {0} to {1}",
                    startDate.ToShortDateString(), endDate.ToShortDateString());

                var prices = (await _provider.GetPricesAsync(
                    DateOnly.FromDateTime(startDate),
                    DateOnly.FromDateTime(endDate),
                    currencies).ConfigureAwait(false)).ToList();

                if (prices.Count != 0)
                {
                    FillLocalDatabase(prices);
                    updated = true;
                }
            }

            if (updated)
            {
                var newRecordCount = _priceDatabase.GetFiatData().Query().Count();
                _logger.LogInformation("[FiatHistoryUpdater] Database updated. Total records: {Count} (added {Added})",
                    newRecordCount, newRecordCount - currentRecordCount);
                WeakReferenceMessenger.Default.Send<FiatHistoryPriceUpdatedMessage>();
            }
            else
            {
                _logger.LogInformation("[FiatHistoryUpdater] Already up to date, no new data needed");
            }

            _logger.LogInformation("[FiatHistoryUpdater] Update cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FiatHistoryUpdater] Error during execution");
            throw;
        }
    }

    /// <summary>
    /// Gets the min and max dates of fiat data in the price database.
    /// </summary>
    private (DateTime? minDate, DateTime? maxDate) GetFiatDateRange()
    {
        try
        {
            var fiatData = _priceDatabase.GetFiatData().FindAll().ToList();
            if (fiatData.Count == 0)
                return (null, null);

            return (fiatData.Min(x => x.Date), fiatData.Max(x => x.Date));
        }
        catch (InvalidOperationException)
        {
            return (null, null);
        }
        catch (NotSupportedException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Gets the required start date for fiat history based on the lowest transaction date.
    /// Uses 2020-01-01 as the default, but goes earlier if there are transactions before that.
    /// </summary>
    private DateTime GetRequiredStartDate()
    {
        var startDate = DefaultStartDate;

        try
        {
            var transactions = _localDatabase.GetTransactions().FindAll().ToList();
            if (transactions.Count > 0)
            {
                var lowestTransactionDate = transactions.Min(x => x.Date);
                if (lowestTransactionDate < startDate)
                {
                    startDate = lowestTransactionDate;
                    _logger.LogInformation("[FiatHistoryUpdater] Using earliest transaction date: {0}",
                        startDate.ToShortDateString());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FiatHistoryUpdater] Error getting lowest transaction date, using default");
        }

        // Skip to first weekday
        while (startDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            startDate = startDate.AddDays(1);

        return startDate;
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