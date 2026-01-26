using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Goals.Commands.CopyGoalsFromLastMonth;

/// <summary>
/// Command to copy monthly goals from the previous month to the current month.
/// Goals with the same target configuration are not duplicated.
/// </summary>
public record CopyGoalsFromLastMonthCommand : ICommand<CopyGoalsFromLastMonthResult>
{
    /// <summary>
    /// The current date to copy goals to. Goals will be copied to this month.
    /// </summary>
    public required DateOnly CurrentDate { get; init; }
}

public record CopyGoalsFromLastMonthResult
{
    /// <summary>
    /// Number of goals that were copied.
    /// </summary>
    public required int CopiedCount { get; init; }
}
