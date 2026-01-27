using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.Kernel.Notifications;

namespace Valt.UI.Handlers;

/// <summary>
/// Bridges LivePriceUpdateMessage from INotificationPublisher to WeakReferenceMessenger for UI consumption.
/// </summary>
internal class LivePriceUpdateUIHandler : INotificationHandler<LivePriceUpdateMessage>
{
    public Task HandleAsync(LivePriceUpdateMessage @event)
    {
        WeakReferenceMessenger.Default.Send(@event);
        return Task.CompletedTask;
    }
}
