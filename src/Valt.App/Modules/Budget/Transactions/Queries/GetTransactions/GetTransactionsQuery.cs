using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery : IQuery<TransactionsDTO>
{
    public string[]? AccountIds { get; init; }
    public string[]? CategoryIds { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? SearchTerm { get; init; }
}
