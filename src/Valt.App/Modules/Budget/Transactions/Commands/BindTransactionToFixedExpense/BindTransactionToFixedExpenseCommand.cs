using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.BindTransactionToFixedExpense;

public record BindTransactionToFixedExpenseCommand : ICommand<Unit>
{
    public required string TransactionId { get; init; }
    public required string FixedExpenseId { get; init; }
    public required DateOnly ReferenceDate { get; init; }
}
