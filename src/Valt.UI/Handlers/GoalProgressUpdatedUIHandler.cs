using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.Goals.Services;

namespace Valt.UI.Handlers;

/// <summary>
/// Bridges GoalProgressUpdated from INotificationPublisher to WeakReferenceMessenger for UI consumption.
/// </summary>
internal class GoalProgressUpdatedUIHandler : INotificationHandler<GoalProgressUpdated>
{
    public Task HandleAsync(GoalProgressUpdated @event)
    {
        WeakReferenceMessenger.Default.Send(@event);
        return Task.CompletedTask;
    }
}
