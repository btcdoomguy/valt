using Valt.Core.Modules.Budget.Transactions;

namespace Valt.Core.Modules.Budget.FixedExpenses.Contracts;

public interface IFixedExpenseRecordService
{
    Task BindFixedExpenseToTransactionAsync(TransactionFixedExpenseReference fixedExpenseReference,
        TransactionId transactionId);
    
    Task UnbindFixedExpenseFromTransactionAsync(TransactionFixedExpenseReference fixedExpenseReference);
    
    Task UnbindAllTransactionsFromFixedExpenseAsync(FixedExpenseId fixedExpenseId);
    
    Task DeleteRecordsRelatedToTransactionAsync(TransactionId transactionId);
    
    Task IgnoreFixedExpenseAsync(TransactionFixedExpenseReference fixedExpenseReference);
    Task UndoIgnoreFixedExpenseAsync(TransactionFixedExpenseReference fixedExpenseReference);
    Task MarkFixedExpenseAsPaidAsync(TransactionFixedExpenseReference fixedExpenseReference);
    Task UnmarkFixedExpenseAsPaidAsync(TransactionFixedExpenseReference fixedExpenseReference);
}