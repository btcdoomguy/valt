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
    private readonly ConcurrentDictionary<Type, object> _handlerCache = new();

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var queryType = query.GetType();
        var wrapper = (IQueryHandlerWrapper<TResult>)_handlerCache.GetOrAdd(
            queryType,
            static t => Activator.CreateInstance(typeof(QueryHandlerWrapper<,>).MakeGenericType(t, typeof(TResult)))!);

        return wrapper.HandleAsync(query, _serviceProvider, ct);
    }

    private interface IQueryHandlerWrapper<TResult>
    {
        Task<TResult> HandleAsync(IQuery<TResult> query, IServiceProvider serviceProvider, CancellationToken ct);
    }

    private sealed class QueryHandlerWrapper<TQuery, TResult> : IQueryHandlerWrapper<TResult>
        where TQuery : IQuery<TResult>
    {
        public Task<TResult> HandleAsync(IQuery<TResult> query, IServiceProvider serviceProvider, CancellationToken ct)
        {
            var handler = serviceProvider.GetService<IQueryHandler<TQuery, TResult>>()
                ?? throw new InvalidOperationException(
                    $"No handler registered for query type '{typeof(TQuery).Name}'.");

            return handler.HandleAsync((TQuery)query, ct);
        }
    }
}
