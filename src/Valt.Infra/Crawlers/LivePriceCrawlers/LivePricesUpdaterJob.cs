using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.App.Kernel.Notifications;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.Crawlers.LivePriceCrawlers;

internal class LivePricesUpdaterJob : IBackgroundJob
{
    private readonly IFiatPriceProviderSelector _fiatPriceProviderSelector;
    private readonly IBitcoinPriceProvider _bitcoinPriceProvider;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalHistoricalPriceProvider _localHistoricalPriceProvider;
    private readonly IConfigurationManager _configurationManager;
    private readonly IPriceDatabaseRatesProvider _ratesProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<LivePricesUpdaterJob> _logger;

    private decimal? _lastClosingPrice;
    private DateOnly? _lastClosingDate;

    private FiatUsdPrice? _fiatUsdPrice;
    private BtcPrice? _btcPrice;

    public string Name => "Live prices updater job";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.LivePricesUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;
    public TimeSpan Interval => TimeSpan.FromSeconds(30);

    public LivePricesUpdaterJob(IFiatPriceProviderSelector fiatPriceProviderSelector,
        IBitcoinPriceProvider bitcoinPriceProvider,
        IPriceDatabase priceDatabase,
        ILocalHistoricalPriceProvider localHistoricalPriceProvider,
        IConfigurationManager configurationManager,
        IPriceDatabaseRatesProvider ratesProvider,
        INotificationPublisher notificationPublisher,
        ILogger<LivePricesUpdaterJob> logger)
    {
        _fiatPriceProviderSelector = fiatPriceProviderSelector;
        _bitcoinPriceProvider = bitcoinPriceProvider;
        _priceDatabase = priceDatabase;
        _localHistoricalPriceProvider = localHistoricalPriceProvider;
        _configurationManager = configurationManager;
        _ratesProvider = ratesProvider;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[LivePricesUpdaterJob] Started");
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[LivePricesUpdaterJob] Starting price update cycle");
        var isUpToDate = false;

        try
        {
            if (!_configurationManager.HasLocalDatabaseOpen)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] No local database open, skipping update");
                return;
            }

            if (!_priceDatabase.HasDatabaseOpen || !_priceDatabase.GetFiatData().Exists(x => true))
            {
                _logger.LogInformation("[LivePricesUpdaterJob] Price database is empty, skipping update");
                return;
            }

            var currencyCodes = _configurationManager.GetAvailableFiatCurrencies();
            if (currencyCodes.Count == 0)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] No currencies configured, skipping update");
                return;
            }

            var currencies = currencyCodes.Select(FiatCurrency.GetFromCode).ToList();

            _logger.LogInformation("[LivePricesUpdaterJob] Fetching prices for {Count} configured currencies: {Currencies}",
                currencies.Count, string.Join(", ", currencies.Select(c => c.Code)));

            var storedRates = await _ratesProvider.GetLatestRatesAsync(stoppingToken).ConfigureAwait(false);
            if (storedRates is not null)
            {
                await _notificationPublisher.PublishAsync(storedRates).ConfigureAwait(false);
                _logger.LogInformation("[LivePricesUpdaterJob] Seeded rates from price database before live fetch");
            }

            var fiatTask = _fiatPriceProviderSelector.GetAsync(currencies);
            var btcTask = _bitcoinPriceProvider.GetAsync();

            await Task.WhenAll(fiatTask, btcTask).ConfigureAwait(false);

            _fiatUsdPrice = await fiatTask.ConfigureAwait(false);
            _btcPrice = await btcTask.ConfigureAwait(false);

            var apiHasBtcUsd = _btcPrice.Items.Any(x => x.CurrencyCode == FiatCurrency.Usd.Code);
            var apiHasAllConfigured = currencyCodes.All(code => _fiatUsdPrice.Items.Any(x => x.Currency.Code == code));
            isUpToDate = _fiatUsdPrice.UpToDate && _btcPrice.UpToDate && apiHasBtcUsd && apiHasAllConfigured;

            if (!apiHasAllConfigured)
            {
                _logger.LogWarning(
                    "[LivePricesUpdaterJob] Live API response is missing {MissingCurrencies} configured currencies; merging with stored rates",
                    string.Join(", ", currencyCodes.Where(code => !_fiatUsdPrice.Items.Any(x => x.Currency.Code == code))));
            }

            _fiatUsdPrice = MergeFiatRates(_fiatUsdPrice, storedRates?.Fiat);
            _btcPrice = MergeBtcPrices(_btcPrice, storedRates?.Btc);

            var btcUsdPrice = _btcPrice.Items.FirstOrDefault(x => x.CurrencyCode == FiatCurrency.Usd.Code);
            if (btcUsdPrice != null)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] BTC/USD: ${Price:N2}", btcUsdPrice.Price);
            }

            foreach (var fiatRate in _fiatUsdPrice.Items)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] USD/{Currency}: {Price:N4}", fiatRate.Currency.Code, fiatRate.Price);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[LivePricesUpdaterJob] Error during live price fetch - using stored rates if available");
            return;
        }

        try
        {
            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);
            var yesterday = localDate.AddDays(-1);

            var refreshLastPrice = _lastClosingDate is null;

            if (_lastClosingDate is not null && _lastClosingDate < DateOnly.FromDateTime(yesterday))
            {
                refreshLastPrice = true;
            }

            if (refreshLastPrice && _priceDatabase.HasDatabaseOpen &&
                _priceDatabase.GetBitcoinData().Exists(x => true))
            {
                var lastDateParsed = _priceDatabase.GetBitcoinData().Max(x => x.Date).Date;
                var previousPrice =
                    await _localHistoricalPriceProvider.GetUsdBitcoinRateAtAsync(DateOnly.FromDateTime(lastDateParsed))
                        .ConfigureAwait(false);

                _lastClosingDate = DateOnly.FromDateTime(lastDateParsed);
                _lastClosingPrice = previousPrice;
            }

            if (_lastClosingPrice is not null)
            {
                _btcPrice.Items.SingleOrDefault(x => x.CurrencyCode == FiatCurrency.Usd.Code)!.SetPreviousPrice(
                    _lastClosingPrice.GetValueOrDefault());
            }

            await _notificationPublisher.PublishAsync(new LivePriceUpdateMessage(_btcPrice, _fiatUsdPrice, isUpToDate));
            _logger.LogInformation("[LivePricesUpdaterJob] Price update completed successfully (up-to-date: {IsUpToDate})", isUpToDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LivePricesUpdaterJob] Error during execution");
            throw;
        }
    }

    private static FiatUsdPrice MergeFiatRates(FiatUsdPrice apiRates, FiatUsdPrice? storedRates)
    {
        if (storedRates is null)
            return apiRates;

        var merged = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in storedRates.Items)
            merged[item.Currency.Code] = item.Price;

        foreach (var item in apiRates.Items)
            merged[item.Currency.Code] = item.Price;

        return new FiatUsdPrice(apiRates.Utc, apiRates.UpToDate,
            merged.Select(x => new FiatUsdPrice.Item(FiatCurrency.GetFromCode(x.Key), x.Value)));
    }

    private static BtcPrice MergeBtcPrices(BtcPrice apiRates, BtcPrice? storedRates)
    {
        if (storedRates is null || apiRates.Items.Any(x => x.CurrencyCode == FiatCurrency.Usd.Code))
            return apiRates;

        var storedUsd = storedRates.Items.FirstOrDefault(x => x.CurrencyCode == FiatCurrency.Usd.Code);
        if (storedUsd is null)
            return apiRates;

        var mergedItems = new HashSet<BtcPrice.Item>(apiRates.Items);
        mergedItems.Add(storedUsd);

        return new BtcPrice(apiRates.Utc, apiRates.UpToDate, mergedItems);
    }
}
