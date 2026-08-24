namespace Valt.App.Modules.LoanReports.DTOs;

public record LiquidationDistanceMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal DistanceToLiquidation { get; init; }
    public required string ClosestLoanName { get; init; }
}
