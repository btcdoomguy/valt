using LiteDB;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Accounts;

internal class AccountGroupRepository : IAccountGroupRepository
{
    private readonly ILocalDatabase _localDatabase;

    public AccountGroupRepository(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<AccountGroup?> GetByIdAsync(AccountGroupId id)
    {
        var entity = _localDatabase.GetAccountGroups().FindById(new ObjectId(id));
        return Task.FromResult(entity?.AsDomainObject());
    }

    public Task<IList<AccountGroup>> GetAllAsync()
    {
        var entities = _localDatabase.GetAccountGroups().FindAll();
        var groups = entities.Select(e => e.AsDomainObject()).OrderBy(g => g.DisplayOrder).ToList();
        return Task.FromResult<IList<AccountGroup>>(groups);
    }

    public Task SaveAsync(AccountGroup group)
    {
        var entity = group.AsEntity();
        _localDatabase.GetAccountGroups().Upsert(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AccountGroupId id)
    {
        var groupIdBson = new ObjectId(id.Value);

        // Unlink all accounts from this group
        var accounts = _localDatabase.GetAccounts().Find(a => a.GroupId == groupIdBson).ToList();
        foreach (var account in accounts)
        {
            account.GroupId = null;
            _localDatabase.GetAccounts().Update(account);
        }

        _localDatabase.GetAccountGroups().Delete(groupIdBson);
        return Task.CompletedTask;
    }
}
