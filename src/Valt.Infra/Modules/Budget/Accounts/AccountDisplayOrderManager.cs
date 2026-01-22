using LiteDB;
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
        var allAccounts = _localDatabase.GetAccounts().FindAll().Where(x => x.Visible).ToList();
        var groups = _localDatabase.GetAccountGroups().FindAll().OrderBy(g => g.DisplayOrder).ToList();

        // Get all unique group IDs (including null for ungrouped)
        var groupIds = groups.Select(g => (ObjectId?)g.Id).ToList();
        groupIds.Add(null); // Add ungrouped

        int globalOrder = 0;

        foreach (var groupId in groupIds)
        {
            // Get accounts in this group, ordered by display order
            var groupAccounts = allAccounts
                .Where(a => a.GroupId == groupId)
                .OrderBy(a => a.DisplayOrder)
                .ToList();

            // Handle move action if it's for an account in this group
            if (action is not null)
            {
                var accountToMove = groupAccounts.SingleOrDefault(x => x.Id == action.AccountId.ToObjectId());

                if (accountToMove is not null)
                {
                    var index = groupAccounts.IndexOf(accountToMove);

                    if (action.Up && index > 0)
                    {
                        groupAccounts.RemoveAt(index);
                        groupAccounts.Insert(index - 1, accountToMove);
                    }

                    if (!action.Up && index < groupAccounts.Count - 1)
                    {
                        groupAccounts.RemoveAt(index);
                        groupAccounts.Insert(index + 1, accountToMove);
                    }
                }
            }

            // Assign sequential display orders within the group
            foreach (var account in groupAccounts)
            {
                if (account.DisplayOrder != globalOrder)
                {
                    account.DisplayOrder = globalOrder;
                    _localDatabase.GetAccounts().Update(account);
                }
                globalOrder++;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the next available display order for accounts in a specific group.
    /// Used when an account is moved to a new group.
    /// </summary>
    public int GetNextDisplayOrderForGroup(ObjectId? groupId)
    {
        var maxOrder = _localDatabase.GetAccounts()
            .Find(a => a.GroupId == groupId && a.Visible)
            .Select(a => a.DisplayOrder)
            .DefaultIfEmpty(-1)
            .Max();

        return maxOrder + 1;
    }
}