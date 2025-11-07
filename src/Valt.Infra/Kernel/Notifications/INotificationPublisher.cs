namespace Valt.Infra.Kernel.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event) where TIntegrationEvent : class, INotification;
}