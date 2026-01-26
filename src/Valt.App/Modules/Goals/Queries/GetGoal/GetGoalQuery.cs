using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetGoal;

/// <summary>
/// Query to get a single goal by ID.
/// </summary>
public record GetGoalQuery : IQuery<GoalDTO?>
{
    /// <summary>
    /// The ID of the goal to retrieve.
    /// </summary>
    public required string GoalId { get; init; }
}
