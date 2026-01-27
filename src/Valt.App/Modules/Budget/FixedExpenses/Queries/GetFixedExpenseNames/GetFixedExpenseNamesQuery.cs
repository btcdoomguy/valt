using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenseNames;

public record GetFixedExpenseNamesQuery : IQuery<FixedExpenseNamesDTO>
{
    /// <summary>
    /// Optional ID of the current fixed expense being edited.
    /// If provided, includes this fixed expense even if disabled.
    /// </summary>
    public string? CurrentFixedExpenseId { get; init; }
}
