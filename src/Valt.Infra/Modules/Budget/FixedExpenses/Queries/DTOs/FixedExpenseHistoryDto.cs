namespace Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

public record FixedExpenseHistoryDto
{
    public required string FixedExpenseId { get; set; }
    public required string FixedExpenseName { get; set; }
    public List<TransactionHistoryItemDto> Transactions { get; set; } = [];
    public List<PriceHistoryItemDto> PriceHistory { get; set; } = [];
}

public record TransactionHistoryItemDto
{
    public required string TransactionId { get; set; }
    public DateOnly Date { get; set; }
    public required string Name { get; set; }
    public required string Amount { get; set; }
    public required string CategoryName { get; set; }
    public string? CategoryIcon { get; set; }
    public required string AccountName { get; set; }
    public string? AccountIcon { get; set; }
    public DateOnly ReferenceDate { get; set; }
}

public record PriceHistoryItemDto
{
    public DateOnly PeriodStart { get; set; }
    public required string Amount { get; set; }
    public required string Period { get; set; }
    public int Day { get; set; }
}
