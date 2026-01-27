using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.BulkChangeCategoryTransactions;

public record BulkChangeCategoryTransactionsCommand : ICommand<BulkChangeCategoryTransactionsResult>
{
    public required string[] TransactionIds { get; init; }
    public required string NewCategoryId { get; init; }
}

public record BulkChangeCategoryTransactionsResult(int UpdatedCount);
