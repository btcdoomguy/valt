using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetGoals;

/// <summary>
/// Query to get goals, optionally filtered by a date that falls within the goal's period.
/// </summary>
public record GetGoalsQuery : IQuery<IReadOnlyList<GoalDTO>>
{
    /// <summary>
    /// Optional filter date. If provided, only goals whose period contains this date will be returned.
    /// </summary>
    public DateOnly? FilterDate { get; init; }
}
