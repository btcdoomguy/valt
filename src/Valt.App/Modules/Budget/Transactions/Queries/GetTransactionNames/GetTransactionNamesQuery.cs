using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactionNames;

public record GetTransactionNamesQuery : IQuery<IReadOnlyList<TransactionNameSearchDTO>>
{
    public required string SearchTerm { get; init; }
}
