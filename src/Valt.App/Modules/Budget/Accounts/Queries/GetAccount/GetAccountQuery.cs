using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;

namespace Valt.App.Modules.Budget.Accounts.Queries.GetAccount;

/// <summary>
/// Query to get a single account by ID.
/// </summary>
public record GetAccountQuery : IQuery<AccountDTO?>
{
    public required string AccountId { get; init; }
}
