using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.UndoIgnoreFixedExpense;

public sealed record UndoIgnoreFixedExpenseCommand(string FixedExpenseId, DateOnly ReferenceDate) : ICommand<Unit>
{
    public UndoIgnoreFixedExpenseCommand(TransactionFixedExpenseReference reference)
        : this(reference.FixedExpenseId.Value, reference.ReferenceDate)
    {
    }
}
