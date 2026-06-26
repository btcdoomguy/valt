using Valt.App.Kernel.Notifications;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;

public record BitcoinHistoryPriceUpdatedMessage() : INotification;