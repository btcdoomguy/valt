using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries.GetAccount;

internal sealed class GetAccountHandler : IQueryHandler<GetAccountQuery, AccountDTO?>
{
    private readonly IAccountQueries _accountQueries;

    public GetAccountHandler(IAccountQueries accountQueries)
    {
        _accountQueries = accountQueries;
    }

    public Task<AccountDTO?> HandleAsync(GetAccountQuery query, CancellationToken ct = default)
    {
        return _accountQueries.GetAccountAsync(query.AccountId);
    }
}
