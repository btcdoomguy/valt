using Microsoft.Extensions.DependencyInjection;

namespace Valt.App.Kernel.Queries;

/// <summary>
/// Default implementation of IQueryDispatcher that resolves handlers from the service provider.
/// </summary>
internal sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for query type '{queryType.Name}'.");

        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))
            ?? throw new InvalidOperationException(
                $"HandleAsync method not found on handler for query type '{queryType.Name}'.");

        var result = handleMethod.Invoke(handler, [query, ct]);
        if (result is Task<TResult> task)
        {
            return await task;
        }

        throw new InvalidOperationException(
            $"Failed to invoke handler for query type '{queryType.Name}'.");
    }
}
