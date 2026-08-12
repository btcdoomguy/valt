namespace Valt.App.Modules.LoanReports.DTOs;

public record LoanCostMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal CombinedCost { get; init; }
    public required decimal Interest { get; init; }
    public required decimal Fees { get; init; }
}
