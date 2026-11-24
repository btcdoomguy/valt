using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Notifications;
using Valt.Infra.Kernel.Scopes;

namespace Valt.Infra.Kernel.Notifications;

public class NotificationPublisher : INotificationPublisher
{
    private static readonly ConcurrentDictionary<Type, INotificationHandlerWrapper> WrapperCache = new();

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
        var rootServiceProvider = _contextScope.GetCurrentServiceProvider();

        // Create a scope to properly resolve scoped services (required for MCP context)
        using var scope = rootServiceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var eventType = @event.GetType();
        var wrapper = WrapperCache.GetOrAdd(
            eventType,
            static t => (INotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapper<>).MakeGenericType(t))!);

        await wrapper.PublishAsync(@event, scopedProvider, _logger);
    }

    private interface INotificationHandlerWrapper
    {
        Task PublishAsync(INotification @event, IServiceProvider scopedProvider, ILogger logger);
    }

    private sealed class NotificationHandlerWrapper<TEvent> : INotificationHandlerWrapper
        where TEvent : class, INotification
    {
        public async Task PublishAsync(INotification @event, IServiceProvider scopedProvider, ILogger logger)
        {
            var handlers = scopedProvider.GetServices<INotificationHandler<TEvent>>();
            var typedEvent = (TEvent)@event;

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(typedEvent);
                }
                catch (Exception ex)
                {
                    var handlerTypeName = handler.GetType().Name;
                    logger.LogError(ex, $"Error during execution of NotificationHandler {handlerTypeName}");
                    throw;
                }
            }
        }
    }
}