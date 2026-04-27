using Valt.Core.Kernel.Abstractions;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Contracts;

public interface ITransactionRepository : IRepository
{
    Task<Transaction?> GetTransactionByIdAsync(TransactionId transactionId);
    Task<IReadOnlyList<Transaction>> GetTransactionsByIdsAsync(IReadOnlyList<TransactionId> transactionIds);
    Task SaveTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(TransactionId transactionId);
    Task<bool> HasAnyTransactionAsync(AccountId accountId);
    Task<IEnumerable<Transaction>> GetTransactionsByGroupIdAsync(GroupId groupId);
    Task DeleteTransactionsByGroupIdAsync(GroupId groupId);
}