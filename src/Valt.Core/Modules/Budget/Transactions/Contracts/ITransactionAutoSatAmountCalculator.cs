namespace Valt.Core.Modules.Budget.Transactions.Contracts;

public interface ITransactionAutoSatAmountCalculator
{
    Task UpdateAutoSatAmountAsync(TransactionId transactionId);
    Task UpdateAutoSatAmountAsync(TransactionId[] transactionIds);
}