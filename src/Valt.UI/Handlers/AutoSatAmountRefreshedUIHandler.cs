using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.Budget.Transactions.Messages;

namespace Valt.UI.Handlers;

/// <summary>
/// Bridges AutoSatAmountRefreshed from INotificationPublisher to WeakReferenceMessenger for UI consumption.
/// </summary>
internal class AutoSatAmountRefreshedUIHandler : INotificationHandler<AutoSatAmountRefreshed>
{
    public Task HandleAsync(AutoSatAmountRefreshed @event)
    {
        WeakReferenceMessenger.Default.Send(@event);
        return Task.CompletedTask;
    }
}
