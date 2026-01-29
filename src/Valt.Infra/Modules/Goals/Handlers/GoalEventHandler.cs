using Valt.App.Modules.Goals.Contracts;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Goals.Events;

namespace Valt.Infra.Modules.Goals.Handlers;

internal class GoalEventHandler :
    IDomainEventHandler<GoalCreatedEvent>,
    IDomainEventHandler<GoalUpdatedEvent>,
    IDomainEventHandler<GoalDeletedEvent>
{
    private readonly IGoalProgressState _progressState;

    public GoalEventHandler(IGoalProgressState progressState)
    {
        _progressState = progressState;
    }

    public Task HandleAsync(GoalCreatedEvent @event)
    {
        _progressState.MarkAsStale();
        return Task.CompletedTask;
    }

    public Task HandleAsync(GoalUpdatedEvent @event)
    {
        _progressState.MarkAsStale();
        return Task.CompletedTask;
    }

    public Task HandleAsync(GoalDeletedEvent @event)
    {
        _progressState.MarkAsStale();
        return Task.CompletedTask;
    }
}
