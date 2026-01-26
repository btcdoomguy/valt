using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Contracts;

public interface ITransactionQueries
{
    Task<TransactionsDTO> GetTransactionsAsync(TransactionQueryFilter filter);
    Task<IReadOnlyList<TransactionNameSearchDTO>> GetTransactionNamesAsync(string searchTerm);
}
