using System.Collections;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valt.Infra.Kernel.Scopes;

namespace Valt.Infra.Kernel.Notifications;

public class NotificationPublisher : INotificationPublisher
{
    private readonly IContextScope _contextScope;
    private readonly ILogger<NotificationPublisher> _logger;
    private readonly ConcurrentDictionary<Type, List<Type>> _handlerTypesByEventType = new();
    
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

        IEnumerable<object> handlers;

        if (!_handlerTypesByEventType.TryGetValue(eventType, out var handlerTypes))
        {
            var resolvedHandlers = (IEnumerable)currentServiceProvider.GetServices(handlerInterfaceType);
            var handlerInstances = resolvedHandlers.Cast<object>().ToList();
            var extractedHandlerTypes = handlerInstances.Select(h => h.GetType()).ToList();

            _handlerTypesByEventType[eventType] = extractedHandlerTypes;

            handlers = handlerInstances;
        }
        else
        {
            handlers = handlerTypes.Select(ht => ActivatorUtilities.CreateInstance(currentServiceProvider, ht)).ToList();
        }

        foreach (dynamic handler in handlers)
        {
            try
            {
                await handler.HandleAsync((dynamic)@event);
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