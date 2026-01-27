namespace Valt.App.Kernel.Queries;

/// <summary>
/// Dispatches queries to their handlers.
/// </summary>
public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
