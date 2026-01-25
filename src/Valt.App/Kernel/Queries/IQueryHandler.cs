namespace Valt.App.Kernel.Queries;

/// <summary>
/// Handles a query of type TQuery and returns TResult.
/// Queries are read operations and don't return Result types - they either succeed or throw.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
