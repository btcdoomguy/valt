using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries;

internal sealed class GetAccountSummariesHandler : IQueryHandler<GetAccountSummariesQuery, AccountSummariesDTO>
{
    private readonly IAccountQueries _accountQueries;

    public GetAccountSummariesHandler(IAccountQueries accountQueries)
    {
        _accountQueries = accountQueries;
    }

    public Task<AccountSummariesDTO> HandleAsync(GetAccountSummariesQuery query, CancellationToken ct = default)
    {
        return _accountQueries.GetAccountSummariesAsync(query.ShowHiddenAccounts);
    }
}
