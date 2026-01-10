using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.Crawlers.LivePriceCrawlers;

internal class LivePricesUpdaterJob : IBackgroundJob
{
    private readonly IFiatPriceProviderSelector _fiatPriceProviderSelector;
    private readonly IBitcoinPriceProvider _bitcoinPriceProvider;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalHistoricalPriceProvider _localHistoricalPriceProvider;
    private readonly ConfigurationManager _configurationManager;
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
        ConfigurationManager configurationManager,
        ILogger<LivePricesUpdaterJob> logger)
    {
        _fiatPriceProviderSelector = fiatPriceProviderSelector;
        _bitcoinPriceProvider = bitcoinPriceProvider;
        _priceDatabase = priceDatabase;
        _localHistoricalPriceProvider = localHistoricalPriceProvider;
        _configurationManager = configurationManager;
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
            // Skip if no local database is open (we need it for currency configuration)
            if (!_configurationManager.HasLocalDatabaseOpen)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] No local database open, skipping update");
                return;
            }

            // Skip if price database is not open or empty
            if (!_priceDatabase.HasDatabaseOpen || _priceDatabase.GetFiatData().Query().Count() == 0)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] Price database is empty, skipping update");
                return;
            }

            // Get all available configured currencies
            var currencyCodes = _configurationManager.GetAvailableFiatCurrencies();
            if (currencyCodes.Count == 0)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] No currencies configured, skipping update");
                return;
            }

            var currencies = currencyCodes.Select(FiatCurrency.GetFromCode).ToList();

            _logger.LogInformation("[LivePricesUpdaterJob] Fetching prices for {Count} configured currencies: {Currencies}",
                currencies.Count, string.Join(", ", currencies.Select(c => c.Code)));

            // Fetch fiat and BTC prices in parallel
            // The fiat price selector handles splitting currencies between providers
            var fiatTask = _fiatPriceProviderSelector.GetAsync(currencies);
            var btcTask = _bitcoinPriceProvider.GetAsync();

            await Task.WhenAll(fiatTask, btcTask).ConfigureAwait(false);

            _fiatUsdPrice = fiatTask.Result;
            _btcPrice = btcTask.Result;

            isUpToDate = _fiatUsdPrice.UpToDate && _btcPrice.UpToDate;

            // Log BTC price
            var btcUsdPrice = _btcPrice.Items.FirstOrDefault(x => x.CurrencyCode == "USD");
            if (btcUsdPrice != null)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] BTC/USD: ${Price:N2}", btcUsdPrice.Price);
            }

            // Log fiat rates
            foreach (var fiatRate in _fiatUsdPrice.Items)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] USD/{Currency}: {Price:N4}", fiatRate.Currency.Code, fiatRate.Price);
            }
        }
        catch (Exception ex)
        {
            //error on crawlers - possibly internet connection issue or external issues - fallback to last known prices
            _logger.LogError(ex,
                "[LivePricesUpdaterJob] Error during crawling prices - falling back to last known prices");

            if (_fiatUsdPrice is null || _btcPrice is null)
            {
                await LastKnownPricesAsync();
                return;
            }
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
                _priceDatabase.GetBitcoinData().Query().Count() > 0)
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

            WeakReferenceMessenger.Default.Send(new LivePriceUpdateMessage(_btcPrice, _fiatUsdPrice, isUpToDate));
            _logger.LogInformation("[LivePricesUpdaterJob] Price update completed successfully (up-to-date: {IsUpToDate})", isUpToDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LivePricesUpdaterJob] Error during execution");
            throw;
        }
    }

    private async Task LastKnownPricesAsync()
    {
        _logger.LogInformation("[LivePricesUpdaterJob] Loading last known prices from database");

        if (!_priceDatabase.HasDatabaseOpen)
        {
            _logger.LogError("[LivePricesUpdaterJob] Price database not open to load last known prices");
            return;
        }

        if (_priceDatabase.GetBitcoinData().Query().Count() == 0)
        {
            _logger.LogError("[LivePricesUpdaterJob] Bitcoin history not loaded");
            return;
        }

        var btcLastDateStored = DateOnly.FromDateTime(_priceDatabase.GetBitcoinData().Max(x => x.Date).Date);
        var btcLastPriceStored =
            await _localHistoricalPriceProvider.GetUsdBitcoinRateAtAsync(btcLastDateStored).ConfigureAwait(false)!;

        _logger.LogInformation("[LivePricesUpdaterJob] Using stored BTC price from {Date}: ${Price:N2}",
            btcLastDateStored, btcLastPriceStored.Value);

        var fiatLastDateStored = DateOnly.FromDateTime(_priceDatabase.GetFiatData().Max(x => x.Date).Date);
        var fiatLastPricesStored = await _localHistoricalPriceProvider.GetAllFiatRatesAtAsync(fiatLastDateStored)
            .ConfigureAwait(false);

        _logger.LogInformation("[LivePricesUpdaterJob] Using stored fiat rates from {Date} ({Count} currencies)",
            fiatLastDateStored, fiatLastPricesStored.Count());

        WeakReferenceMessenger.Default.Send(new LivePriceUpdateMessage(
            new BtcPrice(btcLastDateStored.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local), false,
                new[] { new BtcPrice.Item(FiatCurrency.Usd.Code, btcLastPriceStored.Value, btcLastPriceStored.Value) }),
            new FiatUsdPrice(fiatLastDateStored.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local), false,
                fiatLastPricesStored.Select(x => new FiatUsdPrice.Item(x.Currency, x.Rate))), false));

        _logger.LogInformation("[LivePricesUpdaterJob] Fallback to stored prices completed");
    }
}