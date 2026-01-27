namespace Valt.App.Kernel.Commands;

/// <summary>
/// Marker interface for commands that return a result of type TResult.
/// </summary>
public interface ICommand<TResult>;

/// <summary>
/// Marker interface for commands that don't return a value (return Unit).
/// </summary>
public interface ICommand : ICommand<Unit>;
