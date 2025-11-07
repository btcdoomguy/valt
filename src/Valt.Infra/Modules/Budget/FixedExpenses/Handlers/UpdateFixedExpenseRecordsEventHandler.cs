using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.FixedExpenses.Events;
using Valt.Core.Modules.Budget.Transactions.Events;

namespace Valt.Infra.Modules.Budget.FixedExpenses.Handlers;

public class UpdateFixedExpenseRecordsEventHandler : IDomainEventHandler<TransactionBoundToFixedExpenseEvent>,
    IDomainEventHandler<TransactionUnboundFromFixedExpenseEvent>, IDomainEventHandler<FixedExpenseDeletedEvent>,
    IDomainEventHandler<TransactionDeletedEvent>
{
    private readonly IFixedExpenseRecordService _fixedExpenseRecordService;

    public UpdateFixedExpenseRecordsEventHandler(IFixedExpenseRecordService fixedExpenseRecordService)
    {
        _fixedExpenseRecordService = fixedExpenseRecordService;
    }

    public async Task HandleAsync(TransactionBoundToFixedExpenseEvent @event)
    {
        await _fixedExpenseRecordService.BindFixedExpenseToTransactionAsync(@event.FixedExpenseReference,
            @event.TransactionId);
    }

    public async Task HandleAsync(TransactionUnboundFromFixedExpenseEvent @event)
    {
        await _fixedExpenseRecordService.UnbindFixedExpenseFromTransactionAsync(@event.FixedExpenseReference);
    }

    public async Task HandleAsync(FixedExpenseDeletedEvent @event)
    {
        await _fixedExpenseRecordService.UnbindAllTransactionsFromFixedExpenseAsync(@event.FixedExpense.Id);
    }

    public async Task HandleAsync(TransactionDeletedEvent @event)
    {
        await _fixedExpenseRecordService.DeleteRecordsRelatedToTransactionAsync(@event.Transaction.Id);
    }
}