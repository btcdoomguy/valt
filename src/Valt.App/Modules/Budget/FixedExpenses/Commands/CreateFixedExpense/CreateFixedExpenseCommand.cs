using Valt.App.Kernel.Commands;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;

public record CreateFixedExpenseCommand : ICommand<CreateFixedExpenseResult>
{
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

    public required IReadOnlyList<FixedExpenseRangeInputDTO> Ranges { get; init; }
    public required bool Enabled { get; init; }
}

public record CreateFixedExpenseResult(string FixedExpenseId);
