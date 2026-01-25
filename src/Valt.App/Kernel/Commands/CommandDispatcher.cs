using Microsoft.Extensions.DependencyInjection;

namespace Valt.App.Kernel.Commands;

/// <summary>
/// Default implementation of ICommandDispatcher that resolves handlers from the service provider.
/// </summary>
internal sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<TResult>> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));

        var handler = _serviceProvider.GetService(handlerType);
        if (handler is null)
        {
            return Result<TResult>.Failure(
                "HANDLER_NOT_FOUND",
                $"No handler registered for command type '{commandType.Name}'.");
        }

        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync));
        if (handleMethod is null)
        {
            return Result<TResult>.Failure(
                "HANDLER_METHOD_NOT_FOUND",
                $"HandleAsync method not found on handler for command type '{commandType.Name}'.");
        }

        var result = handleMethod.Invoke(handler, [command, ct]);
        if (result is Task<Result<TResult>> task)
        {
            return await task;
        }

        return Result<TResult>.Failure(
            "HANDLER_INVOCATION_FAILED",
            $"Failed to invoke handler for command type '{commandType.Name}'.");
    }
}
