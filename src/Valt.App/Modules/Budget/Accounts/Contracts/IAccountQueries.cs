using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Contracts;

/// <summary>
/// Group of common queries for accounts
/// </summary>
public interface IAccountQueries
{
    Task<AccountDTO?> GetAccountAsync(string accountId);
    Task<AccountSummariesDTO> GetAccountSummariesAsync(bool showHiddenAccounts);
    Task<IEnumerable<AccountDTO>> GetAccountsAsync(bool showHiddenAccounts);
    Task<IEnumerable<AccountGroupDTO>> GetAccountGroupsAsync();
}
