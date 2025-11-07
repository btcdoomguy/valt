namespace Valt.Infra.Kernel.Notifications;

public interface INotificationHandler<in TEvent> where TEvent : class, INotification
{
    Task HandleAsync(TEvent @event);
}