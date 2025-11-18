using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;

namespace Valt.Infra.Modules.Budget.Accounts.Queries;

/// <summary>
/// Group of common queries for accounts
/// </summary>
public interface IAccountQueries
{
    Task<AccountSummariesDTO> GetAccountSummariesAsync(bool showHiddenAccounts);
    Task<IEnumerable<AccountDTO>> GetAccountsAsync(bool showHiddenAccounts);
}