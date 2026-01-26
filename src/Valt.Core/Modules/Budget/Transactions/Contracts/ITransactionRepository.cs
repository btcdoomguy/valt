using Valt.Core.Kernel.Abstractions;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Contracts;

public interface ITransactionRepository : IRepository
{
    Task<Transaction?> GetTransactionByIdAsync(TransactionId transactionId);
    Task SaveTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(TransactionId transactionId);
    Task<bool> HasAnyTransactionAsync(AccountId accountId);
    Task<IEnumerable<Transaction>> GetTransactionsByGroupIdAsync(GroupId groupId);
    Task DeleteTransactionsByGroupIdAsync(GroupId groupId);
}