using Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

namespace Valt.Infra.Modules.Budget.Transactions.Queries;

public interface ITransactionQueries
{
    Task<TransactionsDTO> GetTransactionsAsync(TransactionQueryFilter filter);
    Task<IReadOnlyList<TransactionNameSearchDTO>> GetTransactionNamesAsync(string searchTerm);
}