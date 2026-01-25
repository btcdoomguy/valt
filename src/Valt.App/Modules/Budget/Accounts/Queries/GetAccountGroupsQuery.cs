using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries;

public record GetAccountGroupsQuery : IQuery<IReadOnlyList<AccountGroupDTO>>;
