namespace Valt.App.Kernel.Commands;

/// <summary>
/// Dispatches commands to their handlers.
/// </summary>
public interface ICommandDispatcher
{
    Task<Result<TResult>> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
}
