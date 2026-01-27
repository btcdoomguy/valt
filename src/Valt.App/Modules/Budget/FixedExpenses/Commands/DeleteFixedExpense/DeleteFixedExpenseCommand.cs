using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.DeleteFixedExpense;

public record DeleteFixedExpenseCommand : ICommand<DeleteFixedExpenseResult>
{
    public required string FixedExpenseId { get; init; }
}

public record DeleteFixedExpenseResult;
