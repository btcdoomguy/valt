using Valt.App.Kernel.Queries;
using Valt.App.Modules.LoanReports.Contracts;
using Valt.App.Modules.LoanReports.DTOs;

namespace Valt.App.Modules.LoanReports.Queries;

internal sealed class GetLoanReportsHandler : IQueryHandler<GetLoanReportsQuery, LoanReportsDataDto>
{
    private readonly ILoanReportsQueries _loanReportsQueries;

    public GetLoanReportsHandler(ILoanReportsQueries loanReportsQueries)
    {
        _loanReportsQueries = loanReportsQueries;
    }

    public Task<LoanReportsDataDto> HandleAsync(GetLoanReportsQuery query, CancellationToken ct = default)
    {
        return _loanReportsQueries.GetLoanReportsAsync(query);
    }
}
