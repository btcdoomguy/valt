using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.BulkEditTransactions;

public record BulkEditTransactionsCommand : ICommand<BulkEditTransactionsResult>
{
    public required string[] TransactionIds { get; init; }
    public string? NewName { get; init; }
    public string? NewCategoryId { get; init; }
}

public record BulkEditTransactionsResult(int UpdatedCount);
