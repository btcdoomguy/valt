using Microsoft.Extensions.Logging;
using Valt.Core.Modules.Assets;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

namespace Valt.Infra.Modules.Assets.PriceProviders;

/// <summary>
/// Price provider that uses the app's live Bitcoin price for BTC assets.
/// Only supports BTC symbol with USD currency.
/// </summary>
internal sealed class LivePricePriceProvider : IAssetPriceProvider
{
    private readonly IBitcoinPriceProvider _bitcoinPriceProvider;
    private readonly ILogger<LivePricePriceProvider> _logger;

    private const string SupportedSymbol = "BTC";
    private const string SupportedCurrency = "USD";

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
        if (!IsSupported(symbol, currencyCode))
        {
            _logger.LogWarning(
                "[LivePrice] Unsupported symbol/currency combination: {Symbol}/{Currency}. Only BTC/USD is supported.",
                symbol, currencyCode);
            return null;
        }

        try
        {
            var btcPrice = await _bitcoinPriceProvider.GetAsync();
            var usdItem = btcPrice.Items.FirstOrDefault(x =>
                x.CurrencyCode.Equals(SupportedCurrency, StringComparison.OrdinalIgnoreCase));

            if (usdItem is null)
            {
                _logger.LogWarning("[LivePrice] USD price not found in BTC price items");
                return null;
            }

            _logger.LogDebug("[LivePrice] Got BTC price: {Price} USD", usdItem.Price);
            return new AssetPriceResult(usdItem.Price, SupportedCurrency, btcPrice.Utc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LivePrice] Error fetching BTC price");
            return null;
        }
    }

    public Task<bool> ValidateSymbolAsync(string symbol)
    {
        var isValid = symbol.Equals(SupportedSymbol, StringComparison.OrdinalIgnoreCase);
        _logger.LogDebug("[LivePrice] Symbol {Symbol} validation: {IsValid}", symbol, isValid);
        return Task.FromResult(isValid);
    }

    private static bool IsSupported(string symbol, string currencyCode)
    {
        return symbol.Equals(SupportedSymbol, StringComparison.OrdinalIgnoreCase) &&
               currencyCode.Equals(SupportedCurrency, StringComparison.OrdinalIgnoreCase);
    }
}
