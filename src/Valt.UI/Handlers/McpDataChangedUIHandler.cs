using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;

namespace Valt.UI.Handlers;

/// <summary>
/// Bridges McpDataChangedNotification from INotificationPublisher to WeakReferenceMessenger for UI consumption.
/// </summary>
internal class McpDataChangedUIHandler : INotificationHandler<McpDataChangedNotification>
{
    public Task HandleAsync(McpDataChangedNotification @event)
    {
        WeakReferenceMessenger.Default.Send(@event);
        return Task.CompletedTask;
    }
}
