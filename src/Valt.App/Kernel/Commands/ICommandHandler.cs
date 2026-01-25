namespace Valt.App.Kernel.Commands;

/// <summary>
/// Handles a command of type TCommand and returns a Result of TResult.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}
