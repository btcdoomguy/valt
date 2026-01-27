using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

public record LivePriceUpdateMessage(
    BtcPrice Btc,
    FiatUsdPrice Fiat,
    bool IsUpToDate) : INotification;