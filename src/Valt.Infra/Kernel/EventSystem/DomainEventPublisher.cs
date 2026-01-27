using System.Collections;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Infra.Kernel.Scopes;

namespace Valt.Infra.Kernel.EventSystem;

public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IContextScope _contextScope;
    private readonly ILogger<DomainEventPublisher> _logger;
    private readonly ConcurrentDictionary<Type, List<Type>> _handlerTypesByEventType = new();

    public DomainEventPublisher(IContextScope contextScope,
        ILogger<DomainEventPublisher> logger)
    {
        _contextScope = contextScope;
        _logger = logger;
    }

    public async Task PublishAsync<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent
    {
        var rootServiceProvider = _contextScope.GetCurrentServiceProvider();

        // Create a scope to properly resolve scoped services (required for MCP context)
        using var scope = rootServiceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var eventType = @event.GetType();
        var handlerInterfaceType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        IEnumerable<object> handlers;

        if (!_handlerTypesByEventType.TryGetValue(eventType, out var handlerTypes))
        {
            var resolvedHandlers = (IEnumerable)scopedProvider.GetServices(handlerInterfaceType);
            var handlerInstances = resolvedHandlers.Cast<object>().ToList();
            var extractedHandlerTypes = handlerInstances.Select(h => h.GetType()).ToList();

            _handlerTypesByEventType[eventType] = extractedHandlerTypes;

            handlers = handlerInstances;
        }
        else
        {
            handlers = handlerTypes.Select(ht => ActivatorUtilities.CreateInstance(scopedProvider, ht)).ToList();
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
                _logger.LogError(ex, $"Error during execution of DomainEventHandler {handlerTypeName}");
                throw;
            }
        }
    }
}