using LiteDB;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.FixedExpenses.Services;

public class FixedExpenseRecordService : IFixedExpenseRecordService
{
    private readonly ILocalDatabase _localDatabase;

    public FixedExpenseRecordService(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }
    
    public async Task BindFixedExpenseToTransactionAsync(TransactionFixedExpenseReference fixedExpenseReference, TransactionId transactionId)
    {
        await ClearAnyFixedExpenseRecordAsync(fixedExpenseReference);

        var fixedExpenseEntity = _localDatabase.GetFixedExpenses()
            .FindById(new ObjectId(fixedExpenseReference.FixedExpenseId.Value));
        var transactionEntity = _localDatabase.GetTransactions().FindById(transactionId.ToObjectId());

        var record = new FixedExpenseRecordEntity()
        {
            FixedExpense = fixedExpenseEntity,
            FixedExpenseRecordStateId = (int)FixedExpenseRecordState.Paid,
            ReferenceDate = fixedExpenseReference.ReferenceDate.ToValtDateTime(),
            Transaction = transactionEntity
        };
        _localDatabase.GetFixedExpenseRecords().Insert(record);
    }

    public async Task UnbindFixedExpenseFromTransactionAsync(TransactionFixedExpenseReference fixedExpenseReference)
    {
        await ClearAnyFixedExpenseRecordAsync(fixedExpenseReference);
    }

    public Task UnbindAllTransactionsFromFixedExpenseAsync(FixedExpenseId fixedExpenseId)
    {
        _localDatabase.GetFixedExpenseRecords()
            .DeleteMany(x => x.FixedExpense.Id == fixedExpenseId.ToObjectId());
        
        return Task.CompletedTask;
    }

    public Task DeleteRecordsRelatedToTransactionAsync(TransactionId transactionId)
    {
        var count = _localDatabase.GetFixedExpenseRecords().DeleteMany(x =>
            x.Transaction != null && x.Transaction.Id == transactionId.ToObjectId());
        
        return Task.CompletedTask;
    }

    public async Task IgnoreFixedExpenseAsync(TransactionFixedExpenseReference fixedExpenseReference)
    {
        await ClearAnyFixedExpenseRecordAsync(fixedExpenseReference);
        
        var fixedExpenseEntity = _localDatabase.GetFixedExpenses()
            .FindById(fixedExpenseReference.FixedExpenseId.ToObjectId());

        var record = new FixedExpenseRecordEntity()
        {
            FixedExpense = fixedExpenseEntity,
            FixedExpenseRecordStateId = (int)FixedExpenseRecordState.Ignored,
            ReferenceDate = fixedExpenseReference.ReferenceDate.ToValtDateTime(),
        };
        _localDatabase.GetFixedExpenseRecords().Insert(record);
    }

    public async Task UndoIgnoreFixedExpenseAsync(TransactionFixedExpenseReference fixedExpenseReference)
    {
        await ClearAnyFixedExpenseRecordAsync(fixedExpenseReference);
    }

    public async Task MarkFixedExpenseAsPaidAsync(TransactionFixedExpenseReference fixedExpenseReference)
    {
        await ClearAnyFixedExpenseRecordAsync(fixedExpenseReference);
        
        var fixedExpenseEntity = _localDatabase.GetFixedExpenses()
            .FindById(fixedExpenseReference.FixedExpenseId.ToObjectId());

        var record = new FixedExpenseRecordEntity()
        {
            FixedExpense = fixedExpenseEntity,
            FixedExpenseRecordStateId = (int)FixedExpenseRecordState.ManuallyPaid,
            ReferenceDate = fixedExpenseReference.ReferenceDate.ToValtDateTime(),
        };
        _localDatabase.GetFixedExpenseRecords().Insert(record);
    }

    public async Task UnmarkFixedExpenseAsPaidAsync(TransactionFixedExpenseReference fixedExpenseReference)
    {
        await ClearAnyFixedExpenseRecordAsync(fixedExpenseReference);
    }

    private Task ClearAnyFixedExpenseRecordAsync(TransactionFixedExpenseReference fixedExpenseReference)
    {
        _localDatabase.GetFixedExpenseRecords().DeleteMany(x =>
            x.FixedExpense.Id == fixedExpenseReference.FixedExpenseId.ToObjectId() &&
            x.ReferenceDate == fixedExpenseReference.ReferenceDate.ToValtDateTime());
        return Task.CompletedTask;
    }
}