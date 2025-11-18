namespace Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

public record LivePriceUpdateMessage(
    IReadOnlyList<BtcPriceMessage> BtcPrices,
    IReadOnlyList<FiatUsdPrice> FiatPrices)
{
}