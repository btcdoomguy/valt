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
        var currentServiceProvider = _contextScope.GetCurrentServiceProvider();
        var eventType = @event.GetType();
        var handlerInterfaceType = typeof(INotificationHandler<>).MakeGenericType(eventType);

        var resolvedHandlers = (IEnumerable)currentServiceProvider.GetServices(handlerInterfaceType);
        var handlers = resolvedHandlers.Cast<object>().ToList();

        foreach (var handler in handlers)
        {
            try
            {
                var handleMethod = handlerInterfaceType.GetMethod("HandleAsync");
                if (handleMethod is not null)
                {
                    var task = (Task?)handleMethod.Invoke(handler, new object[] { @event });
                    if (task is not null)
                        await task;
                }
            }
            catch (Exception ex)
            {
                var handlerTypeName = handler.GetType().Name;
                _logger.LogError(ex, $"Error during execution of NotificationHandler {handlerTypeName}");
                throw;
            }
        }
    }
}