using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.UnbindTransactionFromFixedExpense;

public record UnbindTransactionFromFixedExpenseCommand : ICommand<Unit>
{
    public required string TransactionId { get; init; }
}
