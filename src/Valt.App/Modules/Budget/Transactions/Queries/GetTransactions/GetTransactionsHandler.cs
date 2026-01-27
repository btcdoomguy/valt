using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.Contracts;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactions;

internal sealed class GetTransactionsHandler : IQueryHandler<GetTransactionsQuery, TransactionsDTO>
{
    private readonly ITransactionQueries _transactionQueries;

    public GetTransactionsHandler(ITransactionQueries transactionQueries)
    {
        _transactionQueries = transactionQueries;
    }

    public Task<TransactionsDTO> HandleAsync(GetTransactionsQuery query, CancellationToken ct = default)
    {
        var filter = new TransactionQueryFilter
        {
            AccountIds = query.AccountIds,
            CategoryIds = query.CategoryIds,
            From = query.From,
            To = query.To,
            SearchTerm = query.SearchTerm
        };

        return _transactionQueries.GetTransactionsAsync(filter);
    }
}
