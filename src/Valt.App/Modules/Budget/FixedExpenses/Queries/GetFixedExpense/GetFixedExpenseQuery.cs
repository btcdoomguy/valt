using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpense;

public record GetFixedExpenseQuery : IQuery<FixedExpenseDTO?>
{
    public required string FixedExpenseId { get; init; }
}
