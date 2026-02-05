using Valt.Core.Modules.Assets;

namespace Valt.Infra.Modules.Assets.PriceProviders;

public interface IAssetPriceProviderSelector
{
    IAssetPriceProvider? GetProvider(AssetPriceSource source);
    Task<AssetPriceResult?> GetPriceAsync(AssetPriceSource source, string symbol, string currencyCode);
    Task<bool> ValidateSymbolAsync(AssetPriceSource source, string symbol);
}

internal sealed class AssetPriceProviderSelector : IAssetPriceProviderSelector
{
    private readonly Dictionary<AssetPriceSource, IAssetPriceProvider> _providers;

    public AssetPriceProviderSelector(IEnumerable<IAssetPriceProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Source);
    }

    public IAssetPriceProvider? GetProvider(AssetPriceSource source)
    {
        return _providers.GetValueOrDefault(source);
    }

    public async Task<AssetPriceResult?> GetPriceAsync(AssetPriceSource source, string symbol, string currencyCode)
    {
        var provider = GetProvider(source);
        if (provider is null)
            return null;

        return await provider.GetPriceAsync(symbol, currencyCode);
    }

    public async Task<bool> ValidateSymbolAsync(AssetPriceSource source, string symbol)
    {
        var provider = GetProvider(source);
        if (provider is null)
            return false;

        return await provider.ValidateSymbolAsync(symbol);
    }
}
