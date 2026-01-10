using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

/// <summary>
/// Selects and coordinates multiple fiat price providers to fetch rates for requested currencies.
/// Uses a primary provider (Frankfurter) and falls back to secondary providers for unsupported currencies.
/// </summary>
public interface IFiatPriceProviderSelector
{
    /// <summary>
    /// Gets fiat prices for the requested currencies, using multiple providers if necessary.
    /// Currencies supported by the primary provider are fetched from there, while unsupported
    /// currencies are fetched from secondary providers in parallel.
    /// </summary>
    Task<FiatUsdPrice> GetAsync(IEnumerable<FiatCurrency> currencies);
}
