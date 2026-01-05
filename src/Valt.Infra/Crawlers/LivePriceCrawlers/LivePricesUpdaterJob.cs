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
    private readonly IFiatPriceProvider _fiatPriceProvider;
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

    public LivePricesUpdaterJob(IFiatPriceProvider fiatPriceProvider,
        IBitcoinPriceProvider bitcoinPriceProvider,
        IPriceDatabase priceDatabase,
        ILocalHistoricalPriceProvider localHistoricalPriceProvider,
        ConfigurationManager configurationManager,
        ILogger<LivePricesUpdaterJob> logger)
    {
        _fiatPriceProvider = fiatPriceProvider;
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
        var isUpToDate = false;
        try
        {
            // If price database is empty, skip loading
            if (!_priceDatabase.HasDatabaseOpen || _priceDatabase.GetFiatData().Query().Count() == 0)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] Price database is empty, skipping update");
                return;
            }

            // Get currencies to fetch:
            // - If local database is open, use configuration
            // - If not, use currencies already in price database
            var currencies = GetCurrenciesToFetch();
            if (currencies.Count == 0)
            {
                _logger.LogInformation("[LivePricesUpdaterJob] No currencies to fetch, skipping update");
                return;
            }

            var fiatTask = _fiatPriceProvider.GetAsync(currencies);
            var btcTask = _bitcoinPriceProvider.GetAsync();

            await Task.WhenAll(fiatTask, btcTask).ConfigureAwait(false);

            _fiatUsdPrice = fiatTask.Result;
            _btcPrice = btcTask.Result;
            
            isUpToDate = _fiatUsdPrice.UpToDate && _btcPrice.UpToDate;
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LivePricesUpdaterJob] Error during execution");
            throw;
        }
    }
    
    /// <summary>
    /// Gets the list of currencies to fetch for live prices.
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

            _logger.LogInformation("[LivePricesUpdaterJob] Using {Count} currencies from price database", currencies.Count);
            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LivePricesUpdaterJob] Error getting currencies from price database");
            return new List<string>();
        }
    }

    private async Task LastKnownPricesAsync()
    {
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

        var fiatLastDateStored = DateOnly.FromDateTime(_priceDatabase.GetFiatData().Max(x => x.Date).Date);
        var fiatLastPricesStored = await _localHistoricalPriceProvider.GetAllFiatRatesAtAsync(fiatLastDateStored)
            .ConfigureAwait(false);

        WeakReferenceMessenger.Default.Send(new LivePriceUpdateMessage(
            new BtcPrice(btcLastDateStored.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local), false,
                new[] { new BtcPrice.Item(FiatCurrency.Usd.Code, btcLastPriceStored.Value, btcLastPriceStored.Value) }),
            new FiatUsdPrice(fiatLastDateStored.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local), false,
                fiatLastPricesStored.Select(x => new FiatUsdPrice.Item(x.Currency.Code, x.Rate))), false));
    }
}