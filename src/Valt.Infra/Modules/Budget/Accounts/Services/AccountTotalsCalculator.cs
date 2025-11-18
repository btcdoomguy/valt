using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Accounts.Exceptions;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Accounts.Services;

public class AccountTotalsCalculator : IAccountTotalsCalculator
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IAccountCacheService _accountCacheService;

    public AccountTotalsCalculator(ILocalDatabase localDatabase,
        IAccountCacheService accountCacheService)
    {
        _localDatabase = localDatabase;
        _accountCacheService = accountCacheService;
    }

    public async Task<CalculatedFiatTotals> CalculateFiatTotalAsync(AccountId accountId)
    {
        var account = _localDatabase.GetAccounts().FindById(accountId.AsObjectId());

        if (account.AccountEntityType != AccountEntityType.Fiat)
            throw new WrongAccountTypeException(accountId);

        var accountObjectId = accountId.AsObjectId();

        var accountCache = _localDatabase.GetAccountCaches().FindById(accountObjectId);

        if (accountCache is null)
        {
            await _accountCacheService.CalculateTotalsForAccountAsync(accountId);
            
            accountCache = _localDatabase.GetAccountCaches().FindById(accountObjectId);
        }

        return new CalculatedFiatTotals(accountCache.Total, accountCache.CurrentTotal);
    }

    public async Task<CalculatedBtcTotals> CalculateBtcTotalAsync(AccountId accountId)
    {
        var account = _localDatabase.GetAccounts().FindById(accountId.AsObjectId());

        if (account.AccountEntityType != AccountEntityType.Bitcoin)
            throw new WrongAccountTypeException(accountId);

        var accountObjectId = accountId.AsObjectId();

        var accountCache = _localDatabase.GetAccountCaches().FindById(accountObjectId);
        
        if (accountCache is null)
        {
            await _accountCacheService.CalculateTotalsForAccountAsync(accountId);
            
            accountCache = _localDatabase.GetAccountCaches().FindById(accountObjectId);
        }

        return new CalculatedBtcTotals(Convert.ToInt64(accountCache.Total), Convert.ToInt64(accountCache.CurrentTotal));
    }
}