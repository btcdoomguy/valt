using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.IgnoreFixedExpense;

public sealed record IgnoreFixedExpenseCommand(string FixedExpenseId, DateOnly ReferenceDate) : ICommand<Unit>
{
    public IgnoreFixedExpenseCommand(TransactionFixedExpenseReference reference)
        : this(reference.FixedExpenseId.Value, reference.ReferenceDate)
    {
    }
}
