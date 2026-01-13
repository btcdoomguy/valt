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
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<FiatHistoryUpdaterJob> _logger;

    private readonly IFiatHistoricalDataProvider? _initialSeedProvider;
    private readonly IFiatHistoricalDataProvider? _regularProvider;
    private readonly IFiatHistoricalDataProvider? _fallbackProvider;

    public string Name => "Fiat history updater job";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.FiatHistoryUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;
    public TimeSpan Interval => TimeSpan.FromSeconds(120);

    public FiatHistoryUpdaterJob(
        IPriceDatabase priceDatabase,
        ILocalDatabase localDatabase,
        IEnumerable<IFiatHistoricalDataProvider> providers,
        IConfigurationManager configurationManager,
        ILogger<FiatHistoryUpdaterJob> logger)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _configurationManager = configurationManager;
        _logger = logger;

        var providersList = providers.ToList();
        _initialSeedProvider = providersList.FirstOrDefault(p => p.InitialDownloadSource && !p.IsFallbackProvider);
        _regularProvider = providersList.FirstOrDefault(p => !p.InitialDownloadSource && !p.IsFallbackProvider);
        _fallbackProvider = providersList.FirstOrDefault(p => p.IsFallbackProvider);
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
            if (!CanRun())
                return;

            var currencies = GetConfiguredCurrencies();
            if (currencies.Count == 0)
            {
                _logger.LogInformation("[FiatHistoryUpdater] No currencies configured, skipping update");
                return;
            }

            var currentRecordCount = _priceDatabase.GetFiatData().Query().Count();
            _logger.LogInformation("[FiatHistoryUpdater] Current fiat price records: {Count}", currentRecordCount);

            var requiredStartDate = GetRequiredStartDate();
            var endDate = CalculateEndDate();

            _logger.LogInformation("[FiatHistoryUpdater] Date range: {StartDate} to {EndDate}",
                requiredStartDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var (currenciesWithData, currenciesWithoutData) = CategorizeCurrencies(currencies);
            LogCurrencyCategories(currenciesWithData, currenciesWithoutData);

            var updated = false;

            updated |= await ProcessCurrenciesWithoutDataAsync(currenciesWithoutData, requiredStartDate, endDate);
            updated |= await ProcessCurrenciesWithDataAsync(currenciesWithData, requiredStartDate, endDate);

            LogUpdateResult(updated, currentRecordCount);
            _logger.LogInformation("[FiatHistoryUpdater] Update cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FiatHistoryUpdater] Error during execution");
            throw;
        }
    }

    #region Initialization Checks

    private bool CanRun()
    {
        if (!_configurationManager.HasLocalDatabaseOpen)
        {
            _logger.LogInformation("[FiatHistoryUpdater] No local database open, skipping update");
            return false;
        }

        if (!_priceDatabase.HasDatabaseOpen)
        {
            _logger.LogInformation("[FiatHistoryUpdater] Price database not open, skipping update");
            return false;
        }

        return true;
    }

    private List<FiatCurrency> GetConfiguredCurrencies()
    {
        var currencyCodes = _configurationManager.GetAvailableFiatCurrencies();
        var currencies = currencyCodes.Select(FiatCurrency.GetFromCode).ToList();

        if (currencies.Count > 0)
        {
            _logger.LogInformation("[FiatHistoryUpdater] Configured currencies: {Currencies}",
                string.Join(", ", currencies.Select(c => c.Code)));
        }

        return currencies;
    }

    #endregion

    #region Currency Categorization

    private (List<FiatCurrency> withData, List<FiatCurrency> withoutData) CategorizeCurrencies(
        IEnumerable<FiatCurrency> currencies)
    {
        var withData = new List<FiatCurrency>();
        var withoutData = new List<FiatCurrency>();

        foreach (var currency in currencies)
        {
            // Skip USD as it's the base currency (all rates are USD-based)
            if (currency == FiatCurrency.Usd)
                continue;

            if (HasDataForCurrency(currency))
                withData.Add(currency);
            else
                withoutData.Add(currency);
        }

        return (withData, withoutData);
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

    private void LogCurrencyCategories(List<FiatCurrency> withData, List<FiatCurrency> withoutData)
    {
        _logger.LogInformation("[FiatHistoryUpdater] Currencies with existing data: {Currencies}",
            withData.Count > 0 ? string.Join(", ", withData.Select(c => c.Code)) : "none");
        _logger.LogInformation("[FiatHistoryUpdater] Currencies needing initial seed: {Currencies}",
            withoutData.Count > 0 ? string.Join(", ", withoutData.Select(c => c.Code)) : "none");
    }

    #endregion

    #region Date Calculations

    private DateTime CalculateEndDate()
    {
        var utcNow = DateTime.UtcNow;
        var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);
        return SkipWeekendsBackward(localDate.AddDays(-1));
    }

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
                    _logger.LogInformation("[FiatHistoryUpdater] Using earliest transaction date: {Date}",
                        startDate.ToString("yyyy-MM-dd"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FiatHistoryUpdater] Error getting lowest transaction date, using default");
        }

        return SkipWeekendsForward(startDate);
    }

    private (DateTime? minDate, DateTime? maxDate) GetFiatDateRange()
    {
        try
        {
            var fiatData = _priceDatabase.GetFiatData().FindAll().ToList();
            if (fiatData.Count == 0)
                return (null, null);

            return (fiatData.Min(x => x.Date), fiatData.Max(x => x.Date));
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    private static DateTime SkipWeekendsForward(DateTime date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);
        return date;
    }

    private static DateTime SkipWeekendsBackward(DateTime date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(-1);
        return date;
    }

    #endregion

    #region Data Processing

    private async Task<bool> ProcessCurrenciesWithoutDataAsync(
        List<FiatCurrency> currencies,
        DateTime startDate,
        DateTime endDate)
    {
        if (currencies.Count == 0)
            return false;

        var updated = false;

        // Process with initial seed provider
        if (_initialSeedProvider is not null)
        {
            var supportedCurrencies = currencies
                .Where(c => _initialSeedProvider.SupportedCurrencies.Contains(c))
                .ToList();

            if (supportedCurrencies.Count > 0)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Using {Provider} for initial seed of {Count} currencies",
                    _initialSeedProvider.Name, supportedCurrencies.Count);
                updated |= await FetchAndStorePricesAsync(_initialSeedProvider, supportedCurrencies, startDate, endDate);
            }

            // Use fallback for unsupported currencies
            var unsupportedCurrencies = currencies
                .Where(c => !_initialSeedProvider.SupportedCurrencies.Contains(c))
                .ToList();

            if (unsupportedCurrencies.Count > 0 && _fallbackProvider is not null)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Using fallback {Provider} for initial seed of {Count} unsupported currencies: {Currencies}",
                    _fallbackProvider.Name, unsupportedCurrencies.Count,
                    string.Join(", ", unsupportedCurrencies.Select(c => c.Code)));
                updated |= await FetchAndStorePricesAsync(_fallbackProvider, unsupportedCurrencies, startDate, endDate);
            }
        }
        else if (_fallbackProvider is not null)
        {
            // No initial seed provider, use fallback for all
            _logger.LogInformation("[FiatHistoryUpdater] Using fallback {Provider} for initial seed of {Count} currencies",
                _fallbackProvider.Name, currencies.Count);
            updated |= await FetchAndStorePricesAsync(_fallbackProvider, currencies, startDate, endDate);
        }
        else
        {
            _logger.LogWarning("[FiatHistoryUpdater] No initial seed or fallback provider available");
        }

        return updated;
    }

    private async Task<bool> ProcessCurrenciesWithDataAsync(
        List<FiatCurrency> currencies,
        DateTime requiredStartDate,
        DateTime endDate)
    {
        if (currencies.Count == 0)
            return false;

        var (minFiatDate, maxFiatDate) = GetFiatDateRange();

        if (minFiatDate.HasValue && maxFiatDate.HasValue)
        {
            _logger.LogInformation("[FiatHistoryUpdater] Current fiat date range: {MinDate} to {MaxDate}",
                minFiatDate.Value.ToString("yyyy-MM-dd"), maxFiatDate.Value.ToString("yyyy-MM-dd"));
        }

        var updated = false;

        // Fill backward gap using initial seed provider
        updated |= await FillBackwardGapAsync(currencies, requiredStartDate, minFiatDate);

        // Fill forward using regular provider
        updated |= await FillForwardDataAsync(currencies, maxFiatDate, requiredStartDate, endDate);

        return updated;
    }

    private async Task<bool> FillBackwardGapAsync(
        List<FiatCurrency> currencies,
        DateTime requiredStartDate,
        DateTime? minFiatDate)
    {
        if (minFiatDate is null || requiredStartDate >= minFiatDate.Value)
            return false;

        var gapEndDate = SkipWeekendsBackward(minFiatDate.Value.AddDays(-1));

        if (requiredStartDate > gapEndDate)
            return false;

        var updated = false;

        if (_initialSeedProvider is not null)
        {
            var supportedCurrencies = currencies
                .Where(c => _initialSeedProvider.SupportedCurrencies.Contains(c))
                .ToList();

            if (supportedCurrencies.Count > 0)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Filling backward gap using {Provider} from {StartDate} to {EndDate}",
                    _initialSeedProvider.Name, requiredStartDate.ToString("yyyy-MM-dd"), gapEndDate.ToString("yyyy-MM-dd"));
                updated |= await FetchAndStorePricesAsync(_initialSeedProvider, supportedCurrencies, requiredStartDate, gapEndDate);
            }

            // Use fallback for unsupported currencies
            var unsupportedCurrencies = currencies
                .Where(c => !_initialSeedProvider.SupportedCurrencies.Contains(c))
                .ToList();

            if (unsupportedCurrencies.Count > 0 && _fallbackProvider is not null)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Filling backward gap using fallback {Provider} for {Count} unsupported currencies: {Currencies}",
                    _fallbackProvider.Name, unsupportedCurrencies.Count,
                    string.Join(", ", unsupportedCurrencies.Select(c => c.Code)));
                updated |= await FetchAndStorePricesAsync(_fallbackProvider, unsupportedCurrencies, requiredStartDate, gapEndDate);
            }
        }
        else if (_fallbackProvider is not null)
        {
            _logger.LogInformation("[FiatHistoryUpdater] Filling backward gap using fallback {Provider} from {StartDate} to {EndDate}",
                _fallbackProvider.Name, requiredStartDate.ToString("yyyy-MM-dd"), gapEndDate.ToString("yyyy-MM-dd"));
            updated |= await FetchAndStorePricesAsync(_fallbackProvider, currencies, requiredStartDate, gapEndDate);
        }
        else
        {
            _logger.LogWarning("[FiatHistoryUpdater] No initial seed or fallback provider available for backward gap filling");
        }

        return updated;
    }

    private async Task<bool> FillForwardDataAsync(
        List<FiatCurrency> currencies,
        DateTime? maxFiatDate,
        DateTime requiredStartDate,
        DateTime endDate)
    {
        // Use .Date to normalize to midnight, ensuring proper date-only comparison
        var startDate = SkipWeekendsForward(maxFiatDate?.AddDays(1).Date ?? requiredStartDate);

        if (startDate > endDate)
            return false;

        var updated = false;

        if (_regularProvider is not null)
        {
            var supportedCurrencies = currencies
                .Where(c => _regularProvider.SupportedCurrencies.Contains(c))
                .ToList();

            if (supportedCurrencies.Count > 0)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Using {Provider} from {StartDate} to {EndDate}",
                    _regularProvider.Name, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                updated |= await FetchAndStorePricesAsync(_regularProvider, supportedCurrencies, startDate, endDate);
            }

            // Use fallback for unsupported currencies
            var unsupportedCurrencies = currencies
                .Where(c => !_regularProvider.SupportedCurrencies.Contains(c))
                .ToList();

            if (unsupportedCurrencies.Count > 0 && _fallbackProvider is not null)
            {
                _logger.LogInformation("[FiatHistoryUpdater] Using fallback {Provider} for {Count} unsupported currencies: {Currencies}",
                    _fallbackProvider.Name, unsupportedCurrencies.Count,
                    string.Join(", ", unsupportedCurrencies.Select(c => c.Code)));
                updated |= await FetchAndStorePricesAsync(_fallbackProvider, unsupportedCurrencies, startDate, endDate);
            }
        }
        else if (_fallbackProvider is not null)
        {
            _logger.LogInformation("[FiatHistoryUpdater] Using fallback {Provider} from {StartDate} to {EndDate}",
                _fallbackProvider.Name, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            updated |= await FetchAndStorePricesAsync(_fallbackProvider, currencies, startDate, endDate);
        }
        else
        {
            _logger.LogWarning("[FiatHistoryUpdater] No regular or fallback provider available");
        }

        return updated;
    }

    private async Task<bool> FetchAndStorePricesAsync(
        IFiatHistoricalDataProvider provider,
        List<FiatCurrency> currencies,
        DateTime startDate,
        DateTime endDate)
    {
        var prices = (await provider.GetPricesAsync(
            DateOnly.FromDateTime(startDate),
            DateOnly.FromDateTime(endDate),
            currencies).ConfigureAwait(false)).ToList();

        if (prices.Count == 0)
            return false;

        StorePrices(prices, provider.Name);
        return true;
    }

    #endregion

    #region Database Operations

    private void StorePrices(List<IFiatHistoricalDataProvider.FiatPriceData> prices, string providerName)
    {
        if (prices.Count == 0)
            return;

        var minDate = prices.Min(p => p.Date).ToValtDateTime();
        var maxDate = prices.Max(p => p.Date).ToValtDateTime();

        var existingEntries = _priceDatabase.GetFiatData()
            .Find(x => x.Date >= minDate && x.Date <= maxDate)
            .Select(x => (x.Date, x.Currency))
            .ToHashSet();

        var entries = new List<FiatDataEntity>();

        foreach (var price in prices)
        {
            var date = price.Date.ToValtDateTime();

            foreach (var currencyData in price.Data)
            {
                var currencyCode = currencyData.Currency.Code;

                if (existingEntries.Contains((date, currencyCode)))
                {
                    _logger.LogDebug("[FiatHistoryUpdater] Skipping duplicate entry for {Date} {Currency}",
                        date.ToString("yyyy-MM-dd"), currencyCode);
                    continue;
                }

                _logger.LogInformation("[FiatHistoryUpdater] [{Source}] Adding price {Price} for {Date} for {Currency}",
                    providerName, currencyData.Price, date.ToString("yyyy-MM-dd"), currencyCode);

                entries.Add(new FiatDataEntity
                {
                    Date = date,
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

    private void LogUpdateResult(bool updated, int previousRecordCount)
    {
        if (updated)
        {
            var newRecordCount = _priceDatabase.GetFiatData().Query().Count();
            _logger.LogInformation("[FiatHistoryUpdater] Database updated. Total records: {Count} (added {Added})",
                newRecordCount, newRecordCount - previousRecordCount);
            WeakReferenceMessenger.Default.Send<FiatHistoryPriceUpdatedMessage>();
        }
        else
        {
            _logger.LogInformation("[FiatHistoryUpdater] Already up to date, no new data needed");
        }
    }

    #endregion
}
