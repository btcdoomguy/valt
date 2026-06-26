using Valt.App.Modules.Goals.Contracts;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Goals.Events;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Modules.Goals.Handlers;

internal class GoalEventHandler :
    IDomainEventHandler<GoalCreatedEvent>,
    IDomainEventHandler<GoalUpdatedEvent>,
    IDomainEventHandler<GoalDeletedEvent>
{
    private readonly IGoalProgressState _progressState;
    private readonly BackgroundJobManager _backgroundJobManager;

    public GoalEventHandler(
        IGoalProgressState progressState,
        BackgroundJobManager backgroundJobManager)
    {
        _progressState = progressState;
        _backgroundJobManager = backgroundJobManager;
    }

    public Task HandleAsync(GoalCreatedEvent @event)
    {
        _progressState.MarkAsStale();
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
        return Task.CompletedTask;
    }

    public Task HandleAsync(GoalUpdatedEvent @event)
    {
        _progressState.MarkAsStale();
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
        return Task.CompletedTask;
    }

    public Task HandleAsync(GoalDeletedEvent @event)
    {
        _progressState.MarkAsStale();
        _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
        return Task.CompletedTask;
    }
}
