using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries;

internal sealed class GetAccountGroupsHandler : IQueryHandler<GetAccountGroupsQuery, IReadOnlyList<AccountGroupDTO>>
{
    private readonly IAccountQueries _accountQueries;

    public GetAccountGroupsHandler(IAccountQueries accountQueries)
    {
        _accountQueries = accountQueries;
    }

    public async Task<IReadOnlyList<AccountGroupDTO>> HandleAsync(GetAccountGroupsQuery query, CancellationToken ct = default)
    {
        var groups = await _accountQueries.GetAccountGroupsAsync();
        return groups.ToList();
    }
}
