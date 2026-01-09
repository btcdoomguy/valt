using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
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
    private readonly IEnumerable<IFiatHistoricalDataProvider> _providers;
    private readonly ConfigurationManager _configurationManager;
    private readonly ILogger<FiatHistoryUpdaterJob> _logger;

    public string Name => "Fiat history updater job";

    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.FiatHistoryUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;

    public TimeSpan Interval => TimeSpan.FromSeconds(120);


    public FiatHistoryUpdaterJob(IPriceDatabase priceDatabase,
        ILocalDatabase localDatabase,
        IEnumerable<IFiatHistoricalDataProvider> providers,
        ConfigurationManager configurationManager,
        ILogger<FiatHistoryUpdaterJob> logger)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _providers = providers;
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
            var currencyCodes = _configurationManager.GetAvailableFiatCurrencies();
            if (currencyCodes.Count == 0)
            {
                _logger.LogInformation("[FiatHistoryUpdater] No currencies configured, skipping update");
                return;
            }

            var currencies = currencyCodes.Select(FiatCurrency.GetFromCode).ToList();

            _logger.LogInformation("[FiatHistoryUpdater] Configured currencies: {Currencies}",
                string.Join(", ", currencies.Select(c => c.Code)));

            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);
            var endDate = localDate.AddDays(-1);

            // Skip weekends for end date
            while (endDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                endDate = endDate.AddDays(-1);

            // Calculate the required start date based on the lowest transaction date
            var requiredStartDate = GetRequiredStartDate();
            _logger.LogInformation("[FiatHistoryUpdater] Required start date: {StartDate}", requiredStartDate.ToString("yyyy-MM-dd"));

            // Separate currencies into those that need initial seed vs those that need updates
            var currenciesWithData = new List<FiatCurrency>();
            var currenciesWithoutData = new List<FiatCurrency>();

            foreach (var currency in currencies)
            {
                if (HasDataForCurrency(currency))
                {
                    currenciesWithData.Add(currency);
                }
                else
                {
                    currenciesWithoutData.Add(currency);
                }
            }

            _logger.LogInformation("[FiatHistoryUpdater] Currencies with existing data: {Currencies}",
                currenciesWithData.Count > 0 ? string.Join(", ", currenciesWithData.Select(c => c.Code)) : "none");
            _logger.LogInformation("[FiatHistoryUpdater] Currencies needing initial seed: {Currencies}",
                currenciesWithoutData.Count > 0 ? string.Join(", ", currenciesWithoutData.Select(c => c.Code)) : "none");

            var updated = false;

            // Process currencies without data using initial seed provider
            if (currenciesWithoutData.Count > 0)
            {
                var initialSeedProvider = GetProvider(initialDownloadSource: true);
                if (initialSeedProvider is not null)
                {
                    _logger.LogInformation("[FiatHistoryUpdater] Using {Provider} for initial seed of {Count} currencies",
                        initialSeedProvider.Name, currenciesWithoutData.Count);

                    var prices = (await initialSeedProvider.GetPricesAsync(
                        DateOnly.FromDateTime(requiredStartDate),
                        DateOnly.FromDateTime(endDate),
                        currenciesWithoutData).ConfigureAwait(false)).ToList();

                    if (prices.Count != 0)
                    {
                        FillLocalDatabase(prices, initialSeedProvider.Name);
                        updated = true;
                    }
                }
                else
                {
                    _logger.LogWarning("[FiatHistoryUpdater] No initial seed provider available");
                }
            }

            // Process currencies with existing data using regular provider
            if (currenciesWithData.Count > 0)
            {
                var regularProvider = GetProvider(initialDownloadSource: false);
                if (regularProvider is not null)
                {
                    // Get fiat data date range from prices.db for currencies with data
                    var (minFiatDate, maxFiatDate) = GetFiatDateRange();

                    if (minFiatDate.HasValue && maxFiatDate.HasValue)
                    {
                        _logger.LogInformation("[FiatHistoryUpdater] Current fiat date range: {MinDate} to {MaxDate}",
                            minFiatDate.Value.ToString("yyyy-MM-dd"), maxFiatDate.Value.ToString("yyyy-MM-dd"));
                    }

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

                            var gapPrices = (await regularProvider.GetPricesAsync(
                                DateOnly.FromDateTime(requiredStartDate),
                                DateOnly.FromDateTime(gapEndDate),
                                currenciesWithData).ConfigureAwait(false)).ToList();

                            if (gapPrices.Count != 0)
                            {
                                FillLocalDatabase(gapPrices, regularProvider.Name);
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
                        _logger.LogInformation("[FiatHistoryUpdater] Using {Provider} from {0} to {1}",
                            regularProvider.Name, startDate.ToShortDateString(), endDate.ToShortDateString());

                        var prices = (await regularProvider.GetPricesAsync(
                            DateOnly.FromDateTime(startDate),
                            DateOnly.FromDateTime(endDate),
                            currenciesWithData).ConfigureAwait(false)).ToList();

                        if (prices.Count != 0)
                        {
                            FillLocalDatabase(prices, regularProvider.Name);
                            updated = true;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("[FiatHistoryUpdater] No regular provider available");
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

    private IFiatHistoricalDataProvider? GetProvider(bool initialDownloadSource)
    {
        return _providers.FirstOrDefault(p => p.InitialDownloadSource == initialDownloadSource);
    }

    private bool HasDataForCurrency(FiatCurrency currency)
    {
        try
        {
            return _priceDatabase.GetFiatData().Exists(x => x.Currency == currency.Code);
        }
        catch
        {
            return false;
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

    private void FillLocalDatabase(IEnumerable<IFiatHistoricalDataProvider.FiatPriceData> prices, string providerName)
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
            foreach (var currencyData in price.Data)
            {
                var currencyCode = currencyData.Currency.Code;
                if (existingEntries.Contains((dateToConsider, currencyCode)))
                {
                    _logger.LogDebug(
                        "[FiatHistoryUpdater] Skipping duplicate entry for {Date} {Currency}",
                        dateToConsider.ToString("yyyy-MM-dd"), currencyCode);
                    continue;
                }

                _logger.LogInformation(
                    "[FiatHistoryUpdater] [{Source}] Adding price {CurrencyPrice} for {Date} for {Currency}",
                    providerName, currencyData.Price, dateToConsider.ToString("yyyy-MM-dd"), currencyCode);
                entries.Add(new FiatDataEntity()
                {
                    Date = dateToConsider,
                    Currency = currencyCode,
                    Price = currencyData.Price
                });
            }
        }

        if (entries.Count > 0)
        {
            _priceDatabase.GetFiatData().Insert(entries);
            _priceDatabase.Checkpoint();
        }
    }
}