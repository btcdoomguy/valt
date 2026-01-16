using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Modules.Goals.Handlers;

internal class MarkGoalsStaleEventHandler :
    IDomainEventHandler<TransactionCreatedEvent>,
    IDomainEventHandler<TransactionEditedEvent>,
    IDomainEventHandler<TransactionDeletedEvent>
{
    private readonly IGoalRepository _goalRepository;
    private readonly BackgroundJobManager _backgroundJobManager;

    public MarkGoalsStaleEventHandler(
        IGoalRepository goalRepository,
        BackgroundJobManager backgroundJobManager)
    {
        _goalRepository = goalRepository;
        _backgroundJobManager = backgroundJobManager;
    }

    public async Task HandleAsync(TransactionCreatedEvent @event)
    {
        await _goalRepository.MarkGoalsStaleForDateAsync(@event.Transaction.Date);
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
    }

    public async Task HandleAsync(TransactionEditedEvent @event)
    {
        await _goalRepository.MarkGoalsStaleForDateAsync(@event.Transaction.Date);
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
    }

    public async Task HandleAsync(TransactionDeletedEvent @event)
    {
        await _goalRepository.MarkGoalsStaleForDateAsync(@event.Transaction.Date);
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
    }
}
