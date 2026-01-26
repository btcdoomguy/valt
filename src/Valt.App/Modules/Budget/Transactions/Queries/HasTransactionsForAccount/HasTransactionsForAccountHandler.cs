using Valt.App.Kernel.Queries;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Queries.HasTransactionsForAccount;

internal sealed class HasTransactionsForAccountHandler : IQueryHandler<HasTransactionsForAccountQuery, bool>
{
    private readonly ITransactionRepository _transactionRepository;

    public HasTransactionsForAccountHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<bool> HandleAsync(HasTransactionsForAccountQuery query, CancellationToken ct = default)
    {
        var accountId = new AccountId(query.AccountId);
        return await _transactionRepository.HasAnyTransactionAsync(accountId);
    }
}
