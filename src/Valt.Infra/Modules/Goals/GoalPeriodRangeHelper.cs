using Valt.Core.Modules.Goals;

namespace Valt.Infra.Modules.Goals;

/// <summary>
/// Resolves the (From, To) date range for a goal period. Single source of truth shared by
/// the stale-goals query and the contributing-transactions service.
/// </summary>
internal static class GoalPeriodRangeHelper
{
    public static (DateOnly From, DateOnly To) GetRange(DateOnly refDate, GoalPeriods period, DateOnly? startDate = null)
    {
        return period switch
        {
            GoalPeriods.Monthly => (
                new DateOnly(refDate.Year, refDate.Month, 1),
                new DateOnly(refDate.Year, refDate.Month, DateTime.DaysInMonth(refDate.Year, refDate.Month))),
            GoalPeriods.Yearly => (
                startDate ?? new DateOnly(refDate.Year, 1, 1),
                new DateOnly(refDate.Year, 12, 31)),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }
}
