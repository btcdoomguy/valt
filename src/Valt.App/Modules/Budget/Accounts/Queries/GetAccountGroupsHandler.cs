using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Infra.DataAccess;

namespace Valt.App.Modules.Budget.Accounts.Queries;

internal sealed class GetAccountGroupsHandler : IQueryHandler<GetAccountGroupsQuery, IReadOnlyList<AccountGroupDTO>>
{
    private readonly ILocalDatabase _localDatabase;

    public GetAccountGroupsHandler(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<IReadOnlyList<AccountGroupDTO>> HandleAsync(GetAccountGroupsQuery query, CancellationToken ct = default)
    {
        var groups = _localDatabase.GetAccountGroups()
            .FindAll()
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new AccountGroupDTO(g.Id.ToString(), g.Name, g.DisplayOrder))
            .ToList();

        return Task.FromResult<IReadOnlyList<AccountGroupDTO>>(groups);
    }
}
