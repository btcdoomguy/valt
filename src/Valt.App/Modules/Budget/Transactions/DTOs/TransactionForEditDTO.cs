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

    /// <summary>
    /// Auto sat amount details, if applicable.
    /// </summary>
    public AutoSatAmountDTO? AutoSatAmountDetails { get; init; }
}

public record AutoSatAmountDTO
{
    public required bool IsAutoSatAmount { get; init; }
    public required string SatAmountState { get; init; }
    public long? SatAmountSats { get; init; }
}

public record FixedExpenseReferenceDTO
{
    public required string FixedExpenseId { get; init; }
    public required DateOnly ReferenceDate { get; init; }
}
