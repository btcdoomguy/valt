using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Goals.Events;
using Valt.Infra.Modules.Goals.Services;

namespace Valt.Infra.Modules.Goals.Handlers;

internal class GoalEventHandler :
    IDomainEventHandler<GoalCreatedEvent>,
    IDomainEventHandler<GoalUpdatedEvent>,
    IDomainEventHandler<GoalDeletedEvent>
{
    private readonly GoalProgressState _progressState;

    public GoalEventHandler(GoalProgressState progressState)
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
