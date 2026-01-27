using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.BulkRenameTransactions;

public record BulkRenameTransactionsCommand : ICommand<BulkRenameTransactionsResult>
{
    public required string[] TransactionIds { get; init; }
    public required string NewName { get; init; }
}

public record BulkRenameTransactionsResult(int UpdatedCount);
