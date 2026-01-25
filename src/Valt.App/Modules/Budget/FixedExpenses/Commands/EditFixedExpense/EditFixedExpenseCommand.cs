using Valt.App.Kernel.Commands;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.EditFixedExpense;

public record EditFixedExpenseCommand : ICommand<EditFixedExpenseResult>
{
    public required string FixedExpenseId { get; init; }
    public required string Name { get; init; }
    public required string CategoryId { get; init; }

    /// <summary>
    /// Default account ID. Mutually exclusive with Currency.
    /// </summary>
    public string? DefaultAccountId { get; init; }

    /// <summary>
    /// Currency code (e.g., "USD"). Mutually exclusive with DefaultAccountId.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// The new range to add. When editing, ranges after the last fixed expense record are replaced.
    /// </summary>
    public FixedExpenseRangeInputDTO? NewRange { get; init; }

    public required bool Enabled { get; init; }
}

public record EditFixedExpenseResult;
