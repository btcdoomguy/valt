using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Budget.Accounts.Services;

public interface IAccountCacheService
{
    Task CalculateTotalsForAccountAsync(AccountId accountId);
    Task RefreshCurrentTotalsAsync(DateOnly today);
}