using Microsoft.Extensions.Logging;
using Valt.Core.Modules.Assets;
using YahooFinanceApi;

namespace Valt.Infra.Modules.Assets.PriceProviders;

internal sealed class YahooFinancePriceProvider : IAssetPriceProvider
{
    private readonly ILogger<YahooFinancePriceProvider> _logger;

    public AssetPriceSource Source => AssetPriceSource.YahooFinance;

    public YahooFinancePriceProvider(ILogger<YahooFinancePriceProvider> logger)
    {
        _logger = logger;
    }

    public async Task<AssetPriceResult?> GetPriceAsync(string symbol, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return null;

        try
        {
            var securities = await Yahoo.Symbols(symbol)
                .Fields(Field.Symbol, Field.RegularMarketPrice, Field.Currency)
                .QueryAsync();

            if (!securities.TryGetValue(symbol, out var security))
            {
                _logger.LogWarning("[YahooFinance] Symbol {Symbol} not found", symbol);
                return null;
            }

            var price = (decimal)security.RegularMarketPrice;
            var currency = security.Currency ?? "USD";

            _logger.LogDebug("[YahooFinance] Got price for {Symbol}: {Price} {Currency}", symbol, price, currency);

            return new AssetPriceResult(price, currency, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[YahooFinance] Error fetching price for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<bool> ValidateSymbolAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        try
        {
            var securities = await Yahoo.Symbols(symbol)
                .Fields(Field.Symbol)
                .QueryAsync();

            var isValid = securities.ContainsKey(symbol);
            _logger.LogDebug("[YahooFinance] Symbol {Symbol} validation: {IsValid}", symbol, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[YahooFinance] Error validating symbol {Symbol}", symbol);
            return false;
        }
    }
}
