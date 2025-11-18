using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

internal interface IFiatPriceProvider
{
    string Name { get; }
    Task<IReadOnlyList<FiatUsdPrice>> GetAsync();
}