using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Modules.Budget.Transactions.Handlers;

internal class UpdateSatAmountEventHandler : IDomainEventHandler<TransactionCreatedEvent>, IDomainEventHandler<TransactionDetailsChangedEvent>,
    IDomainEventHandler<TransactionEditedEvent>
{
    private readonly BackgroundJobManager _backgroundJobManager;

    public UpdateSatAmountEventHandler(BackgroundJobManager backgroundJobManager)
    {
        _backgroundJobManager = backgroundJobManager;
    }
    
    public Task HandleAsync(TransactionCreatedEvent @event)
    {
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.AutoSatAmountUpdater);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransactionDetailsChangedEvent @event)
    {
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.AutoSatAmountUpdater);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransactionEditedEvent @event)
    {
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.AutoSatAmountUpdater);
        return Task.CompletedTask;
    }
}