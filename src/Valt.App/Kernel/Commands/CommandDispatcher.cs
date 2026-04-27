using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Valt.App.Kernel.Commands;

/// <summary>
/// Default implementation of ICommandDispatcher that resolves handlers from the service provider.
/// Caches handler type/method lookups to avoid per-call reflection overhead.
/// </summary>
internal sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, HandlerCacheEntry> _handlerCache = new();

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<TResult>> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var commandType = command.GetType();
        var cacheEntry = _handlerCache.GetOrAdd(commandType, BuildCacheEntry<TResult>);

        var handler = _serviceProvider.GetService(cacheEntry.HandlerType);
        if (handler is null)
        {
            return Result<TResult>.Failure(
                "HANDLER_NOT_FOUND",
                $"No handler registered for command type '{commandType.Name}'.");
        }

        var result = cacheEntry.HandleMethod.Invoke(handler, [command, ct]);
        if (result is Task<Result<TResult>> task)
        {
            return await task;
        }

        return Result<TResult>.Failure(
            "HANDLER_INVOCATION_FAILED",
            $"Failed to invoke handler for command type '{commandType.Name}'.");
    }

    private static HandlerCacheEntry BuildCacheEntry<TResult>(Type commandType)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync))!;
        return new HandlerCacheEntry(handlerType, handleMethod);
    }

    private sealed record HandlerCacheEntry(Type HandlerType, System.Reflection.MethodInfo HandleMethod);
}
