namespace Valt.App.Modules.Budget.FixedExpenses.DTOs;

public record FixedExpenseHistoryDTO
{
    public required string FixedExpenseId { get; init; }
    public required string FixedExpenseName { get; init; }
    public required IReadOnlyList<TransactionHistoryItemDTO> Transactions { get; init; }
    public required IReadOnlyList<PriceHistoryItemDTO> PriceHistory { get; init; }
}

public record TransactionHistoryItemDTO
{
    public required string TransactionId { get; init; }
    public required DateOnly Date { get; init; }
    public required string Name { get; init; }
    public required string Amount { get; init; }
    public required string CategoryName { get; init; }
    public string? CategoryIcon { get; init; }
    public required string AccountName { get; init; }
    public string? AccountIcon { get; init; }
    public required DateOnly ReferenceDate { get; init; }
}

public record PriceHistoryItemDTO
{
    public required DateOnly PeriodStart { get; init; }
    public required string Amount { get; init; }
    public required string Period { get; init; }
    public required int Day { get; init; }
}
