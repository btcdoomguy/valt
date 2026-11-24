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
    private readonly ConcurrentDictionary<Type, object> _handlerCache = new();

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Result<TResult>> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var commandType = command.GetType();
        var wrapper = (ICommandHandlerWrapper<TResult>)_handlerCache.GetOrAdd(
            commandType,
            static t => Activator.CreateInstance(typeof(CommandHandlerWrapper<,>).MakeGenericType(t, typeof(TResult)))!);

        return wrapper.HandleAsync(command, _serviceProvider, ct);
    }

    private interface ICommandHandlerWrapper<TResult>
    {
        Task<Result<TResult>> HandleAsync(ICommand<TResult> command, IServiceProvider serviceProvider, CancellationToken ct);
    }

    private sealed class CommandHandlerWrapper<TCommand, TResult> : ICommandHandlerWrapper<TResult>
        where TCommand : ICommand<TResult>
    {
        public Task<Result<TResult>> HandleAsync(ICommand<TResult> command, IServiceProvider serviceProvider, CancellationToken ct)
        {
            var handler = serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
            if (handler is null)
            {
                return Task.FromResult(Result<TResult>.Failure(
                    "HANDLER_NOT_FOUND",
                    $"No handler registered for command type '{typeof(TCommand).Name}'."));
            }

            return handler.HandleAsync((TCommand)command, ct);
        }
    }
}
