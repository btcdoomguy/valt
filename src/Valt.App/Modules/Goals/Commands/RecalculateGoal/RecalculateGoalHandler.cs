using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.Notifications;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;

namespace Valt.App.Modules.Goals.Commands.RecalculateGoal;

internal sealed class RecalculateGoalHandler : ICommandHandler<RecalculateGoalCommand, RecalculateGoalResult>
{
    private readonly IGoalRepository _goalRepository;
    private readonly INotificationPublisher _notificationPublisher;

    public RecalculateGoalHandler(
        IGoalRepository goalRepository,
        INotificationPublisher notificationPublisher)
    {
        _goalRepository = goalRepository;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<RecalculateGoalResult>> HandleAsync(
        RecalculateGoalCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.GoalId))
            return Result<RecalculateGoalResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.GoalId), ["Goal ID is required"] }
                }));

        var goal = await _goalRepository.GetByIdAsync(new GoalId(command.GoalId));

        if (goal is null)
            return Result<RecalculateGoalResult>.Failure(
                "GOAL_NOT_FOUND", $"Goal with id {command.GoalId} not found");

        // Only allow recalculation of Completed or Failed goals
        if (goal.State != GoalStates.Completed && goal.State != GoalStates.Failed)
            return Result<RecalculateGoalResult>.Failure(
                "INVALID_STATE", "Only Completed or Failed goals can be recalculated");

        goal.Recalculate();
        await _goalRepository.SaveAsync(goal);
        await _notificationPublisher.PublishAsync(new GoalProgressUpdateRequested());

        return Result<RecalculateGoalResult>.Success(new RecalculateGoalResult());
    }
}
