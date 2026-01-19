namespace Valt.Core.Modules.Goals;

public interface IGoalType
{
    GoalTypeNames TypeName { get; }

    /// <summary>
    /// Indicates whether this goal type depends on historical price data for accurate calculation.
    /// When true, the goal will be marked as stale when price database updates occur.
    /// </summary>
    bool RequiresPriceDataForCalculation { get; }

    /// <summary>
    /// Defines how progress translates to success or failure.
    /// ZeroToSuccess: Progress goes from 0% to 100%, reaching 100% means success (Completed).
    /// DecreasingSuccess: Progress starts at 100% (full budget) and decreases, reaching 0% means failure (Failed).
    /// </summary>
    ProgressionMode ProgressionMode { get; }

    /// <summary>
    /// Determines if this goal type has the same target configuration as another.
    /// Used for duplicate detection when copying goals between periods.
    /// Compares only target parameters, not calculated/progress values.
    /// </summary>
    bool HasSameTargetAs(IGoalType other);

    /// <summary>
    /// Creates a copy of this goal type with calculated values reset to zero.
    /// Used when copying goals to a new period.
    /// </summary>
    IGoalType WithResetProgress();
}
