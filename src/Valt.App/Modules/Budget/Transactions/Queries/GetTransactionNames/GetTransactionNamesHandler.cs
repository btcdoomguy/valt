using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.Contracts;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactionNames;

internal sealed class GetTransactionNamesHandler : IQueryHandler<GetTransactionNamesQuery, IReadOnlyList<TransactionNameSearchDTO>>
{
    private readonly ITransactionQueries _transactionQueries;

    public GetTransactionNamesHandler(ITransactionQueries transactionQueries)
    {
        _transactionQueries = transactionQueries;
    }

    public Task<IReadOnlyList<TransactionNameSearchDTO>> HandleAsync(GetTransactionNamesQuery query, CancellationToken ct = default)
    {
        return _transactionQueries.GetTransactionNamesAsync(query.SearchTerm);
    }
}
