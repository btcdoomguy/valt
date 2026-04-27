using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Valt.App.Kernel.Queries;

/// <summary>
/// Default implementation of IQueryDispatcher that resolves handlers from the service provider.
/// Caches handler type/method lookups to avoid per-call reflection overhead.
/// </summary>
internal sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, HandlerCacheEntry> _handlerCache = new();

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var queryType = query.GetType();
        var cacheEntry = _handlerCache.GetOrAdd(queryType, BuildCacheEntry<TResult>);

        var handler = _serviceProvider.GetService(cacheEntry.HandlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for query type '{queryType.Name}'.");

        var result = cacheEntry.HandleMethod.Invoke(handler, [query, ct]);
        if (result is Task<TResult> task)
        {
            return await task;
        }

        throw new InvalidOperationException(
            $"Failed to invoke handler for query type '{queryType.Name}'.");
    }

    private static HandlerCacheEntry BuildCacheEntry<TResult>(Type queryType)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))!;
        return new HandlerCacheEntry(handlerType, handleMethod);
    }

    private sealed record HandlerCacheEntry(Type HandlerType, System.Reflection.MethodInfo HandleMethod);
}
