namespace Valt.App.Modules.Goals.Contracts;

/// <summary>
/// Holds in-memory state for goal progress updates.
/// Used to avoid repeated database queries - instead, event handlers set the flag
/// and the background job checks this flag to know when to recalculate.
/// </summary>
public interface IGoalProgressState
{
    /// <summary>
    /// Indicates whether there are stale goals that need recalculation.
    /// </summary>
    bool HasStaleGoals { get; }

    /// <summary>
    /// Indicates whether the initial bootstrap run has completed.
    /// </summary>
    bool BootstrapCompleted { get; }

    /// <summary>
    /// Marks that there are stale goals requiring update.
    /// Called by event handlers when transactions or prices change.
    /// </summary>
    void MarkAsStale();

    /// <summary>
    /// Resets the stale flag after goals have been updated.
    /// </summary>
    void ClearStaleFlag();

    /// <summary>
    /// Marks the bootstrap phase as complete.
    /// After this, the job will only check the in-memory flag.
    /// </summary>
    void MarkBootstrapCompleted();
}
