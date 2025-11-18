using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

internal interface IBitcoinPriceProvider
{
    string Name { get; }
    Task<IReadOnlyList<BtcPriceMessage>> GetAsync();
}