using Microsoft.Extensions.Logging;
using Valt.Core.Modules.Assets;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

namespace Valt.Infra.Modules.Assets.PriceProviders;

/// <summary>
/// Price provider for Bitcoin leveraged positions using live BTC prices.
/// Supports any fiat currency by looking up prices from the BTC price provider.
/// </summary>
internal sealed class LivePricePriceProvider : IAssetPriceProvider
{
    private readonly IBitcoinPriceProvider _bitcoinPriceProvider;
    private readonly ILogger<LivePricePriceProvider> _logger;

    public AssetPriceSource Source => AssetPriceSource.LivePrice;

    public LivePricePriceProvider(
        IBitcoinPriceProvider bitcoinPriceProvider,
        ILogger<LivePricePriceProvider> logger)
    {
        _bitcoinPriceProvider = bitcoinPriceProvider;
        _logger = logger;
    }

    public async Task<AssetPriceResult?> GetPriceAsync(string symbol, string currencyCode)
    {
        // Only supports BTC symbols
        if (!symbol.StartsWith("BTC", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[LivePrice] Symbol {Symbol} is not supported. Only BTC symbols are supported.", symbol);
            return null;
        }

        try
        {
            var btcPrice = await _bitcoinPriceProvider.GetAsync();

            // Find price for requested currency (BRL, EUR, USD, etc.)
            var priceItem = btcPrice.Items.FirstOrDefault(x =>
                string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));

            if (priceItem is null)
            {
                _logger.LogWarning("[LivePrice] Currency {CurrencyCode} not found in BTC price data", currencyCode);
                return null;
            }

            _logger.LogDebug("[LivePrice] Got price for {Symbol}: {Price} {Currency}", symbol, priceItem.Price, currencyCode);

            return new AssetPriceResult(priceItem.Price, currencyCode, btcPrice.Utc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LivePrice] Error fetching BTC price for {Symbol} in {CurrencyCode}", symbol, currencyCode);
            return null;
        }
    }

    public Task<bool> ValidateSymbolAsync(string symbol)
    {
        var isValid = symbol.StartsWith("BTC", StringComparison.OrdinalIgnoreCase);
        _logger.LogDebug("[LivePrice] Symbol {Symbol} validation: {IsValid}", symbol, isValid);
        return Task.FromResult(isValid);
    }
}
