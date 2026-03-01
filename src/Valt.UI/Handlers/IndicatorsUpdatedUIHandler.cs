using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Crawlers.Indicators;
using Valt.Infra.Kernel.Notifications;

namespace Valt.UI.Handlers;

/// <summary>
/// Bridges IndicatorsUpdatedMessage from INotificationPublisher to WeakReferenceMessenger for UI consumption.
/// </summary>
internal class IndicatorsUpdatedUIHandler : INotificationHandler<IndicatorsUpdatedMessage>
{
    public Task HandleAsync(IndicatorsUpdatedMessage @event)
    {
        WeakReferenceMessenger.Default.Send(@event);
        return Task.CompletedTask;
    }
}
