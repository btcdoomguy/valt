using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Modules.Goals.Services;

namespace Valt.Infra.Modules.Goals.Handlers;

internal class MarkGoalsStaleEventHandler :
    IDomainEventHandler<TransactionCreatedEvent>,
    IDomainEventHandler<TransactionEditedEvent>,
    IDomainEventHandler<TransactionDeletedEvent>
{
    private readonly IGoalRepository _goalRepository;
    private readonly GoalProgressState _progressState;

    public MarkGoalsStaleEventHandler(
        IGoalRepository goalRepository,
        GoalProgressState progressState)
    {
        _goalRepository = goalRepository;
        _progressState = progressState;
    }

    public async Task HandleAsync(TransactionCreatedEvent @event)
    {
        await _goalRepository.MarkGoalsStaleForDateAsync(@event.Transaction.Date);
        _progressState.MarkAsStale();
    }

    public async Task HandleAsync(TransactionEditedEvent @event)
    {
        await _goalRepository.MarkGoalsStaleForDateAsync(@event.Transaction.Date);
        _progressState.MarkAsStale();
    }

    public async Task HandleAsync(TransactionDeletedEvent @event)
    {
        await _goalRepository.MarkGoalsStaleForDateAsync(@event.Transaction.Date);
        _progressState.MarkAsStale();
    }
}
