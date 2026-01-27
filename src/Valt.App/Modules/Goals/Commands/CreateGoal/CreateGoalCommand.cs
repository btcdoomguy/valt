using Valt.App.Kernel.Commands;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Commands.CreateGoal;

public record CreateGoalCommand : ICommand<CreateGoalResult>
{
    /// <summary>
    /// Reference date for the goal period.
    /// </summary>
    public required DateOnly RefDate { get; init; }

    /// <summary>
    /// Period type: 0=Monthly, 1=Yearly
    /// </summary>
    public required int Period { get; init; }

    /// <summary>
    /// The goal type configuration.
    /// </summary>
    public required GoalTypeInputDTO GoalType { get; init; }
}

public record CreateGoalResult(string GoalId);
