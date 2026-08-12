using Valt.App.Kernel.Queries;
using Valt.App.Modules.LoanReports.DTOs;

namespace Valt.App.Modules.LoanReports.Queries;

public record GetLoanReportsQuery : IQuery<LoanReportsDataDto>
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public decimal? CustomBtcPriceUsd { get; init; }
}
