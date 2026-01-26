using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;

public record FiatHistoryPriceUpdatedMessage() : INotification;