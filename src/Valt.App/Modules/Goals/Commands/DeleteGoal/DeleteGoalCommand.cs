using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Goals.Commands.DeleteGoal;

public record DeleteGoalCommand : ICommand<DeleteGoalResult>
{
    public required string GoalId { get; init; }
}

public record DeleteGoalResult;
