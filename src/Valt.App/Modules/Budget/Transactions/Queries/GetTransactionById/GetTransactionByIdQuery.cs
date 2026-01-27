using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactionById;

/// <summary>
/// Query to get a single transaction by ID for editing.
/// </summary>
public record GetTransactionByIdQuery : IQuery<TransactionForEditDTO?>
{
    public required string TransactionId { get; init; }
}
