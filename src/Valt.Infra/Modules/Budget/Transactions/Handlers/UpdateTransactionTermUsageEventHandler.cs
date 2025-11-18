using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Infra.TransactionTerms;

namespace Valt.Infra.Modules.Budget.Transactions.Handlers;

internal class UpdateTransactionTermUsageEventHandler : IDomainEventHandler<TransactionCreatedEvent>,
    IDomainEventHandler<TransactionDeletedEvent>, IDomainEventHandler<TransactionNameAndCategoryChangedEvent>
{
    private readonly ITransactionTermService _transactionTermService;

    public UpdateTransactionTermUsageEventHandler(ITransactionTermService transactionTermService)
    {
        _transactionTermService = transactionTermService;
    }

    public Task HandleAsync(TransactionCreatedEvent @event)
    {
        _transactionTermService.AddEntry(@event.Transaction.Name, @event.Transaction.CategoryId,
            @event.Transaction.TransactionDetails.FromAccountSatsValue,
            @event.Transaction.TransactionDetails.FromAccountFiatValue);

        return Task.CompletedTask;
    }

    public Task HandleAsync(TransactionDeletedEvent @event)
    {
        var transaction = @event.Transaction;

        _transactionTermService.RemoveEntry(transaction.Name, transaction.CategoryId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransactionNameAndCategoryChangedEvent @event)
    {
        _transactionTermService.RemoveEntry(@event.PreviousTransactionName, @event.PreviousCategoryId);

        _transactionTermService.AddEntry(@event.Transaction.Name, @event.Transaction.CategoryId,
            @event.Transaction.TransactionDetails.FromAccountSatsValue,
            @event.Transaction.TransactionDetails.FromAccountFiatValue);

        return Task.CompletedTask;
    }
}