using Valt.Core.Modules.Assets;

namespace Valt.Infra.Modules.Assets.PriceProviders;

public interface IAssetPriceProvider
{
    AssetPriceSource Source { get; }
    Task<AssetPriceResult?> GetPriceAsync(string symbol, string currencyCode);
    Task<bool> ValidateSymbolAsync(string symbol);
}

public record AssetPriceResult(decimal Price, string CurrencyCode, DateTime FetchedAt);
