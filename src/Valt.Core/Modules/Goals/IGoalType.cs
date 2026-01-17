namespace Valt.Core.Modules.Goals;

public interface IGoalType
{
    GoalTypeNames TypeName { get; }

    /// <summary>
    /// Indicates whether this goal type depends on historical price data for accurate calculation.
    /// When true, the goal will be marked as stale when price database updates occur.
    /// </summary>
    bool RequiresPriceDataForCalculation { get; }
}
