using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valt.Infra.Kernel.Scopes;

namespace Valt.Infra.Kernel.Notifications;

public class NotificationPublisher : INotificationPublisher
{
    private readonly IContextScope _contextScope;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(IContextScope contextScope,
        ILogger<NotificationPublisher> logger)
    {
        _contextScope = contextScope;
        _logger = logger;
    }

    public async Task PublishAsync<TNotification>(TNotification @event) where TNotification : class, INotification
    {
        var serviceProvider = _contextScope.GetCurrentServiceProvider();

        var eventType = @event.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(eventType);
        var handlers = (IEnumerable)serviceProvider.GetServices(handlerType);
        
        foreach (dynamic handler in handlers)
        {
            try
            {
                await handler.HandleAsync((dynamic)@event);
            }
            catch (Exception ex)
            {
                var handlerTypeName = handler.GetType().Name;

                _logger.LogError(ex, $"Error during execution of IntegrationEventHandler {handlerTypeName}");
                throw;
            }
        }
    }
}