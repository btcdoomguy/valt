using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

public interface IBitcoinPriceProvider
{
    string Name { get; }
    Task<BtcPrice> GetAsync();
}