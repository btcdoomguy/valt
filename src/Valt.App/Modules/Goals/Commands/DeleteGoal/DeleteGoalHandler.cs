using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;

namespace Valt.App.Modules.Goals.Commands.DeleteGoal;

internal sealed class DeleteGoalHandler : ICommandHandler<DeleteGoalCommand, DeleteGoalResult>
{
    private readonly IGoalRepository _goalRepository;

    public DeleteGoalHandler(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;
    }

    public async Task<Result<DeleteGoalResult>> HandleAsync(
        DeleteGoalCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.GoalId))
            return Result<DeleteGoalResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.GoalId), ["Goal ID is required"] }
                }));

        var goal = await _goalRepository.GetByIdAsync(new GoalId(command.GoalId));

        if (goal is null)
            return Result<DeleteGoalResult>.Failure(
                "GOAL_NOT_FOUND", $"Goal with id {command.GoalId} not found");

        await _goalRepository.DeleteAsync(goal);

        return Result<DeleteGoalResult>.Success(new DeleteGoalResult());
    }
}
