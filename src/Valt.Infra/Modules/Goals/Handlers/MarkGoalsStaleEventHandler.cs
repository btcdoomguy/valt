using Valt.App.Modules.Goals.Contracts;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Core.Modules.Goals.Contracts;

namespace Valt.Infra.Modules.Goals.Handlers;

internal class MarkGoalsStaleEventHandler :
    IDomainEventHandler<TransactionCreatedEvent>,
    IDomainEventHandler<TransactionEditedEvent>,
    IDomainEventHandler<TransactionDeletedEvent>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IGoalProgressState _progressState;

    public MarkGoalsStaleEventHandler(
        IGoalRepository goalRepository,
        IGoalProgressState progressState)
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
