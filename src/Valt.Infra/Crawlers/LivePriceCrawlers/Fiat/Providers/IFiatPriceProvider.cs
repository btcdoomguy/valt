using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

public interface IFiatPriceProvider
{
    string Name { get; }
    IReadOnlySet<FiatCurrency> SupportedCurrencies { get; }
    Task<FiatUsdPrice> GetAsync(IEnumerable<FiatCurrency> currencies);
}