using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.MarkFixedExpenseAsPaid;

public sealed record MarkFixedExpenseAsPaidCommand(string FixedExpenseId, DateOnly ReferenceDate) : ICommand<Unit>
{
    public MarkFixedExpenseAsPaidCommand(TransactionFixedExpenseReference reference)
        : this(reference.FixedExpenseId.Value, reference.ReferenceDate)
    {
    }
}
