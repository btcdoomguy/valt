using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries.GetAccounts;

public record GetAccountsQuery(bool ShowHiddenAccounts) : IQuery<IReadOnlyList<AccountDTO>>;
