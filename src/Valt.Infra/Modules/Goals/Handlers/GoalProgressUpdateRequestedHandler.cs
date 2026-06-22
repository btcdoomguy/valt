using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.Notifications;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Modules.Goals.Handlers;

internal class GoalProgressUpdateRequestedHandler : INotificationHandler<GoalProgressUpdateRequested>
{
    private readonly BackgroundJobManager _manager;
    private readonly IGoalProgressState _progressState;
    private readonly ILogger<GoalProgressUpdateRequestedHandler> _logger;

    public GoalProgressUpdateRequestedHandler(
        BackgroundJobManager manager,
        IGoalProgressState progressState,
        ILogger<GoalProgressUpdateRequestedHandler> logger)
    {
        _manager = manager;
        _progressState = progressState;
        _logger = logger;
    }

    public Task HandleAsync(GoalProgressUpdateRequested notification)
    {
        _logger.LogInformation("[GoalProgressUpdateRequestedHandler] Marking goals stale and triggering GoalProgressUpdater job");
        _progressState.MarkAsStale();
        _manager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
        return Task.CompletedTask;
    }
}
