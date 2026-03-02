using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Crawlers.Indicators;

public record IndicatorsUpdatedMessage(IndicatorSnapshot Snapshot) : INotification;
