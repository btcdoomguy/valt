using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Accounts;

public class AccountDisplayOrderManager
{
    private readonly ILocalDatabase _localDatabase;

    public AccountDisplayOrderManager(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }
    
    public Task NormalizeDisplayOrdersAsync(AccountOrderAction? action)
    {
        var accounts = _localDatabase.GetAccounts().FindAll().Where(x => x.Visible).OrderBy(x => x.DisplayOrder).ToList();

        if (action is not null)
        {
            var accountToMove = accounts.SingleOrDefault(x => x.Id == action.AccountId.ToObjectId());

            if (accountToMove is not null)
            {
                var index = accounts.IndexOf(accountToMove);

                if (action.Up && index > 0)
                {
                    accounts.RemoveAt(index);
                    accounts.Insert(index - 1, accountToMove);
                }
                
                if (!action.Up && index < accounts.Count - 1)
                {
                    accounts.RemoveAt(index);
                    accounts.Insert(index + 1, accountToMove);
                }
            }
        }

        foreach (var account in accounts)
        {
            var newDisplayOrder = accounts.IndexOf(account);

            if (account.DisplayOrder == newDisplayOrder) 
                continue;
            
            account.DisplayOrder = accounts.IndexOf(account);
            _localDatabase.GetAccounts().Update(account);
        }
        
        return Task.CompletedTask;
    }
}