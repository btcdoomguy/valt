namespace Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

public record FixedExpenseDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? DefaultAccountId { get; set; }
    public string? DefaultAccountName { get; set; }
    public string? Currency { get; set; }
    public required string DisplayCurrency { get; set; }
    
    public HashSet<RangeDto> Ranges { get; set; } = [];
    public bool Enabled { get; set; }

    public RangeDto LatestRange => Ranges.Last();

    public record RangeDto
    {
        public DateOnly PeriodStart { get; set; }
        public decimal? FixedAmount { get; set; }
        public string? FixedAmountFormatted { get; set; }
        public decimal? RangedAmountMin { get; set; }
        public string? RangedAmountMinFormatted { get; set; }
        public decimal? RangedAmountMax { get; set; }
        public string? RangedAmountMaxFormatted { get; set; }
        public int PeriodId { get; set; }
        public required string PeriodDescription { get; set; }
        public int Day { get; set; }
    }
}