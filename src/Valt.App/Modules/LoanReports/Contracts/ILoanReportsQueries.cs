using Valt.App.Kernel.Queries;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.App.Modules.LoanReports.Queries;

namespace Valt.App.Modules.LoanReports.Contracts;

public interface ILoanReportsQueries
{
    Task<LoanReportsDataDto> GetLoanReportsAsync(GetLoanReportsQuery query);
}
