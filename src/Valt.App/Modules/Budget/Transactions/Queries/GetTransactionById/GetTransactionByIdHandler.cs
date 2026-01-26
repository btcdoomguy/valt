using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.Contracts;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactionById;

internal sealed class GetTransactionByIdHandler : IQueryHandler<GetTransactionByIdQuery, TransactionForEditDTO?>
{
    private readonly ITransactionQueries _transactionQueries;

    public GetTransactionByIdHandler(ITransactionQueries transactionQueries)
    {
        _transactionQueries = transactionQueries;
    }

    public Task<TransactionForEditDTO?> HandleAsync(GetTransactionByIdQuery query, CancellationToken ct = default)
    {
        return _transactionQueries.GetTransactionByIdAsync(query.TransactionId);
    }
}
