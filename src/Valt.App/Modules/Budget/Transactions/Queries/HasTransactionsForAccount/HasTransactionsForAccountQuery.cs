using Valt.App.Kernel.Queries;

namespace Valt.App.Modules.Budget.Transactions.Queries.HasTransactionsForAccount;

/// <summary>
/// Query to check if an account has any transactions.
/// Used to prevent deletion of accounts with transactions.
/// </summary>
public record HasTransactionsForAccountQuery : IQuery<bool>
{
    public required string AccountId { get; init; }
}
