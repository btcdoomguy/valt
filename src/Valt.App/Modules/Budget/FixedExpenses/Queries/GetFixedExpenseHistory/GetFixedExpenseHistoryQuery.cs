using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenseHistory;

public record GetFixedExpenseHistoryQuery : IQuery<FixedExpenseHistoryDTO?>
{
    public required string FixedExpenseId { get; init; }
}
