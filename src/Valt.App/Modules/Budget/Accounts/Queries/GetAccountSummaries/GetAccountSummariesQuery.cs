using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries.GetAccountSummaries;

public record GetAccountSummariesQuery(bool ShowHiddenAccounts) : IQuery<AccountSummariesDTO>;
