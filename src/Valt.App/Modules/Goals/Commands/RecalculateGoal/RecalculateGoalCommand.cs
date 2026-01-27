using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Goals.Commands.RecalculateGoal;

/// <summary>
/// Command to force recalculation of a goal that is in Completed or Failed state.
/// </summary>
public record RecalculateGoalCommand : ICommand<RecalculateGoalResult>
{
    public required string GoalId { get; init; }
}

public record RecalculateGoalResult;
