namespace Valt.App.Modules.LoanReports.DTOs;

public record LoanReportsDataDto
{
    public required IReadOnlyList<LoanCostMonthDto> CostMonths { get; init; }
    public required IReadOnlyList<LiquidationDistanceMonthDto> DistanceMonths { get; init; }
    public required bool HasActiveLoans { get; init; }
    public required string PrimaryCurrency { get; init; }
}
