namespace Valt.App.Modules.Budget.Transactions.DTOs;

/// <summary>
/// DTO containing full transaction data needed for editing.
/// </summary>
public record TransactionForEditDTO
{
    public required string Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Name { get; init; }
    public required string CategoryId { get; init; }
    public string? Notes { get; init; }
    public string? GroupId { get; init; }

    /// <summary>
    /// Fixed expense reference, if bound.
    /// </summary>
    public FixedExpenseReferenceDTO? FixedExpenseReference { get; init; }

    /// <summary>
    /// Transaction details (polymorphic based on transaction type).
    /// </summary>
    public required TransactionDetailsDto Details { get; init; }
}

public record FixedExpenseReferenceDTO
{
    public required string FixedExpenseId { get; init; }
    public required DateOnly ReferenceDate { get; init; }
}
