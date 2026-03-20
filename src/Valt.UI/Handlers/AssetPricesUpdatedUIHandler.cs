using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.Assets.Services;

namespace Valt.UI.Handlers;

/// <summary>
/// Bridges AssetPricesUpdated from INotificationPublisher to WeakReferenceMessenger for UI consumption.
/// </summary>
internal class AssetPricesUpdatedUIHandler : INotificationHandler<AssetPricesUpdated>
{
    public Task HandleAsync(AssetPricesUpdated @event)
    {
        WeakReferenceMessenger.Default.Send(@event);
        return Task.CompletedTask;
    }
}
