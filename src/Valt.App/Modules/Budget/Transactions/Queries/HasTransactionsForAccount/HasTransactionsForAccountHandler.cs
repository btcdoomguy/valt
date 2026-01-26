using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Queries.HasTransactionsForAccount;

internal sealed class HasTransactionsForAccountHandler : IQueryHandler<HasTransactionsForAccountQuery, bool>
{
    private readonly ITransactionQueries _transactionQueries;

    public HasTransactionsForAccountHandler(ITransactionQueries transactionQueries)
    {
        _transactionQueries = transactionQueries;
    }

    public Task<bool> HandleAsync(HasTransactionsForAccountQuery query, CancellationToken ct = default)
    {
        return _transactionQueries.HasAnyTransactionAsync(query.AccountId);
    }
}
