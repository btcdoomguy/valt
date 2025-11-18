using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.Budget.Accounts.Contracts;

public interface IAccountRepository : IRepository
{
    Task<Account?> GetAccountByIdAsync(AccountId accountId);
    Task SaveAccountAsync(Account account);
    Task<IList<Account>> GetAccountsAsync();
    Task DeleteAccountAsync(Account account);
}