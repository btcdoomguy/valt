namespace Valt.App.Modules.SpendingAnalytics.DTOs;

public record BurnRateDataDto
{
    public required bool HasData { get; init; }
    public required decimal SpentSoFar { get; init; }
    public required decimal AvgDailySpend { get; init; }
    public decimal? ProjectedMonthEnd { get; init; }
    public required decimal MedianMonthlyExpenses { get; init; }
    public decimal? VsMedianPercent { get; init; }
    public required int DayOfMonth { get; init; }
    public required string PrimaryCurrency { get; init; }
}
