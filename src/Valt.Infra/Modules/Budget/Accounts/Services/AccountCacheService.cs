using LiteDB;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Accounts.Services;

internal class AccountCacheService : IAccountCacheService
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;

    public AccountCacheService(ILocalDatabase localDatabase, IClock clock)
    {
        _localDatabase = localDatabase;
        _clock = clock;
    }

    public async Task CalculateTotalsForAccountAsync(AccountId accountId)
    {
        var accountObjectId = accountId.AsObjectId();
        var account = _localDatabase.GetAccounts().FindById(accountObjectId);

        if (account is null)
            return;

        _localDatabase.GetAccountCaches().Delete(accountObjectId);

        // Single query to get all transactions for this account (as From or To)
        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.FromAccountId == accountObjectId || x.ToAccountId == accountObjectId)
            .ToList();

        decimal total = account.InitialAmount;
        var isFiatAccount = account.AccountEntityType == AccountEntityType.Fiat;

        // Calculate totals in a single pass through the transactions
        foreach (var transaction in transactions)
        {
            if (transaction.FromAccountId == accountObjectId)
            {
                total += isFiatAccount
                    ? transaction.FromFiatAmount.GetValueOrDefault()
                    : transaction.FromSatAmount.GetValueOrDefault();
            }

            if (transaction.ToAccountId == accountObjectId)
            {
                total += isFiatAccount
                    ? transaction.ToFiatAmount.GetValueOrDefault()
                    : transaction.ToSatAmount.GetValueOrDefault();
            }
        }

        var accountCache = new AccountCacheEntity
        {
            Id = accountId.AsObjectId(),
            Total = total,
            CurrentTotal = 0,
            CurrentDate = DateTime.MinValue
        };

        _localDatabase.GetAccountCaches().Insert(accountCache);

        var today = _clock.GetCurrentLocalDate();

        await RefreshCurrentTotalForAccountAsync(accountObjectId, today);
    }

    public async Task RefreshCurrentTotalsAsync(DateOnly today)
    {
        //reprocess account future caches
        var allAccountCaches = _localDatabase.GetAccountCaches().FindAll();

        foreach (var accountCache in allAccountCaches)
        {
            await RefreshCurrentTotalForAccountAsync(accountCache.Id, today);
        }
    }

    /// <summary>
    /// Updates the cache with the current total (for the current day) vs the future totals
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="today"></param>
    /// <returns></returns>
    private Task RefreshCurrentTotalForAccountAsync(ObjectId accountId, DateOnly today)
    {
        var accountCache = _localDatabase.GetAccountCaches().FindById(accountId);

        //only refresh if cache exists or is not up to date
        if (accountCache is null || DateOnly.FromDateTime(accountCache.CurrentDate) >= today)
            return Task.CompletedTask;

        decimal currentTotal = 0;
        if (accountCache.CurrentDate == DateTime.MinValue)
        {
            var account = _localDatabase.GetAccounts().FindById(accountId);
            currentTotal = account.InitialAmount;
        }
        else
        {
            currentTotal = accountCache.CurrentTotal;
        }

        //finds all operations between last date and today
        var transactions = _localDatabase.GetTransactions().Query()
            .Where(x => (x.FromAccountId == accountCache.Id || x.ToAccountId == accountCache.Id) &&
                        x.Date > accountCache.CurrentDate && x.Date <= today.ToValtDateTime())
            .OrderBy(x => x.Date)
            .ToList();

        foreach (var transaction in transactions)
        {
            if (transaction.FromAccountId == accountCache.Id)
            {
                if (transaction.FromFiatAmount is not null)
                    currentTotal += transaction.FromFiatAmount.Value;
                else if (transaction.FromSatAmount is not null)
                    currentTotal += transaction.FromSatAmount.Value;
            }
            else
            {
                if (transaction.ToFiatAmount is not null)
                    currentTotal += transaction.ToFiatAmount.Value;
                else if (transaction.ToSatAmount is not null)
                    currentTotal += transaction.ToSatAmount.Value;
            }
        }

        accountCache.CurrentTotal = currentTotal;
        accountCache.CurrentDate = today.ToValtDateTime();
        _localDatabase.GetAccountCaches().Update(accountCache);
        return Task.CompletedTask;
    }
}