namespace Valt.App.Modules.Budget.FixedExpenses.DTOs;

public record FixedExpenseDTO
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? CategoryIcon { get; init; }
    public string? DefaultAccountId { get; init; }
    public string? DefaultAccountName { get; init; }
    public string? DefaultAccountIcon { get; init; }
    public string? Currency { get; init; }
    public required string DisplayCurrency { get; init; }
    public required bool Enabled { get; init; }
    public required IReadOnlyList<FixedExpenseRangeDTO> Ranges { get; init; }
    public FixedExpenseRangeDTO LatestRange => Ranges[^1];
}

public record FixedExpenseRangeDTO
{
    public required DateOnly PeriodStart { get; init; }
    public decimal? FixedAmount { get; init; }
    public string? FixedAmountFormatted { get; init; }
    public decimal? RangedAmountMin { get; init; }
    public string? RangedAmountMinFormatted { get; init; }
    public decimal? RangedAmountMax { get; init; }
    public string? RangedAmountMaxFormatted { get; init; }
    public required int PeriodId { get; init; }
    public required string PeriodDescription { get; init; }
    public required int Day { get; init; }
}
