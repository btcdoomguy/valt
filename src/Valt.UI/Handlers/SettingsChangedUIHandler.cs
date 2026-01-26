using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Settings;

namespace Valt.UI.Handlers;

/// <summary>
/// Bridges SettingsChangedMessage from INotificationPublisher to WeakReferenceMessenger for UI consumption.
/// </summary>
internal class SettingsChangedUIHandler : INotificationHandler<SettingsChangedMessage>
{
    public Task HandleAsync(SettingsChangedMessage @event)
    {
        WeakReferenceMessenger.Default.Send(@event);
        return Task.CompletedTask;
    }
}
