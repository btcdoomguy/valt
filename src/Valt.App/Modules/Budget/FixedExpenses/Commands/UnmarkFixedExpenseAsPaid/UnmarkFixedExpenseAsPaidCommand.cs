using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.UnmarkFixedExpenseAsPaid;

public sealed record UnmarkFixedExpenseAsPaidCommand(string FixedExpenseId, DateOnly ReferenceDate) : ICommand<Unit>
{
    public UnmarkFixedExpenseAsPaidCommand(TransactionFixedExpenseReference reference)
        : this(reference.FixedExpenseId.Value, reference.ReferenceDate)
    {
    }
}
