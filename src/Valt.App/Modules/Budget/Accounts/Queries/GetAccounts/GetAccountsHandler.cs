using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries.GetAccounts;

internal sealed class GetAccountsHandler : IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDTO>>
{
    private readonly IAccountQueries _accountQueries;

    public GetAccountsHandler(IAccountQueries accountQueries)
    {
        _accountQueries = accountQueries;
    }

    public async Task<IReadOnlyList<AccountDTO>> HandleAsync(GetAccountsQuery query, CancellationToken ct = default)
    {
        var accounts = await _accountQueries.GetAccountsAsync(query.ShowHiddenAccounts);
        return accounts.ToList();
    }
}
