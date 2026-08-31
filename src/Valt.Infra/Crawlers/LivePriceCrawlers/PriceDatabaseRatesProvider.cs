using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.Crawlers.LivePriceCrawlers;

internal sealed class PriceDatabaseRatesProvider : IPriceDatabaseRatesProvider
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<PriceDatabaseRatesProvider> _logger;

    public PriceDatabaseRatesProvider(
        IPriceDatabase priceDatabase,
        IConfigurationManager configurationManager,
        ILogger<PriceDatabaseRatesProvider> logger)
    {
        _priceDatabase = priceDatabase;
        _configurationManager = configurationManager;
        _logger = logger;
    }

    public Task<LivePriceUpdateMessage?> GetLatestRatesAsync(CancellationToken cancellationToken = default)
    {
        if (!_priceDatabase.HasDatabaseOpen)
        {
            _logger.LogInformation("[PriceDatabaseRatesProvider] Price database not open, cannot load latest rates");
            return Task.FromResult<LivePriceUpdateMessage?>(null);
        }

        if (!_configurationManager.HasLocalDatabaseOpen)
        {
            _logger.LogInformation("[PriceDatabaseRatesProvider] Local database not open, cannot load configured currencies");
            return Task.FromResult<LivePriceUpdateMessage?>(null);
        }

        var btcPrice = GetLatestBtcPrice();
        var fiatRates = GetLatestFiatRates();

        if (btcPrice is null && fiatRates.Count == 0)
        {
            _logger.LogInformation("[PriceDatabaseRatesProvider] No stored rates available");
            return Task.FromResult<LivePriceUpdateMessage?>(null);
        }

        var now = DateTime.UtcNow;
        var message = new LivePriceUpdateMessage(
            btcPrice ?? new BtcPrice(now, false, []),
            new FiatUsdPrice(now, false, fiatRates),
            false);

        return Task.FromResult<LivePriceUpdateMessage?>(message);
    }

    private BtcPrice? GetLatestBtcPrice()
    {
        try
        {
            if (!_priceDatabase.GetBitcoinData().Exists(x => true))
                return null;

            var entry = _priceDatabase.GetBitcoinData()
                .FindAll()
                .OrderByDescending(x => x.Date)
                .First();

            return new BtcPrice(entry.Date, false,
            [
                new BtcPrice.Item(FiatCurrency.Usd.Code, entry.Price, entry.Price)
            ]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PriceDatabaseRatesProvider] Error loading latest BTC price");
            return null;
        }
    }

    private IReadOnlySet<FiatUsdPrice.Item> GetLatestFiatRates()
    {
        var rates = new HashSet<FiatUsdPrice.Item>();

        try
        {
            var currencyCodes = _configurationManager.GetAvailableFiatCurrencies();
            if (currencyCodes.Count == 0)
            {
                _logger.LogInformation("[PriceDatabaseRatesProvider] No configured currencies");
                return rates;
            }

            foreach (var code in currencyCodes)
            {
                var currency = FiatCurrency.GetFromCode(code);

                // USD is the base currency, so its rate is always 1.
                if (currency == FiatCurrency.Usd)
                {
                    rates.Add(new FiatUsdPrice.Item(currency, 1m));
                    continue;
                }

                var entry = _priceDatabase.GetFiatData()
                    .Find(x => x.Currency == code)
                    .OrderByDescending(x => x.Date)
                    .FirstOrDefault();

                if (entry is not null)
                {
                    rates.Add(new FiatUsdPrice.Item(FiatCurrency.GetFromCode(entry.Currency), entry.Price));
                }
                else
                {
                    _logger.LogWarning("[PriceDatabaseRatesProvider] No stored rate found for {Currency}", code);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PriceDatabaseRatesProvider] Error loading latest fiat rates");
        }

        return rates;
    }
}
