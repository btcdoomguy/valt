using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

public interface IFiatPriceProvider
{
    string Name { get; }
    Task<FiatUsdPrice> GetAsync();
}