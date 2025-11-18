using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Crawlers.LivePriceCrawlers;

internal class LivePricesUpdaterJob : IBackgroundJob
{
    private readonly FrankfurterFiatRateProvider _frankfurterFiatRateProvider;
    private readonly CoinbaseProvider _coinbaseProvider;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalHistoricalPriceProvider _localHistoricalPriceProvider;
    private readonly ILogger<LivePricesUpdaterJob> _logger;
    
    private decimal? _lastClosingPrice;
    private DateOnly? _lastClosingDate;
    
    public string Name => "Live prices updater job";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.LivePricesUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.PriceDatabase;
    public TimeSpan Interval => TimeSpan.FromSeconds(30);

    public LivePricesUpdaterJob(FrankfurterFiatRateProvider frankfurterFiatRateProvider,
        CoinbaseProvider coinbaseProvider,
        IPriceDatabase priceDatabase,
        ILocalHistoricalPriceProvider localHistoricalPriceProvider,
        ILogger<LivePricesUpdaterJob> logger)
    {
        _frankfurterFiatRateProvider = frankfurterFiatRateProvider;
        _coinbaseProvider = coinbaseProvider;
        _priceDatabase = priceDatabase;
        _localHistoricalPriceProvider = localHistoricalPriceProvider;
        _logger = logger;
    }
    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[LivePricesUpdaterJob] Started");
        return Task.CompletedTask;
    }
    
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            var fiatTask = _frankfurterFiatRateProvider.GetAsync();
            var btcTask = _coinbaseProvider.GetAsync();

            await Task.WhenAll(fiatTask, btcTask).ConfigureAwait(false);

            var fiatResponse = fiatTask.Result;
            var btcResponse = btcTask.Result;
            
            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);
            var yesterday = localDate.AddDays(-1);

            var refreshLastPrice = _lastClosingDate is null;

            if (_lastClosingDate is not null && _lastClosingDate < DateOnly.FromDateTime(yesterday))
            {
                refreshLastPrice = true;
            }

            if (refreshLastPrice && _priceDatabase.HasDatabaseOpen && _priceDatabase.GetBitcoinData().Query().Count() > 0)
            {
                var lastDateParsed = _priceDatabase.GetBitcoinData().Max(x => x.Date).Date;
                var previousPrice =
                    await _localHistoricalPriceProvider.GetUsdBitcoinRateAtAsync(DateOnly.FromDateTime(lastDateParsed)).ConfigureAwait(false);

                _lastClosingDate = DateOnly.FromDateTime(lastDateParsed);
                _lastClosingPrice = previousPrice;
            }

            if (_lastClosingPrice is not null)
            {
                btcResponse.SingleOrDefault(x => x.CurrencyCode == "USD")!.SetPreviousPrice(_lastClosingPrice.GetValueOrDefault());
            }

            WeakReferenceMessenger.Default.Send(new LivePriceUpdateMessage(btcResponse, fiatResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LivePricesUpdaterJob] Error during execution");
            throw;
        }
    }
}