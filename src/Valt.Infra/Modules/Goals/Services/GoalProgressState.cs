using Valt.App.Modules.Goals.Contracts;

namespace Valt.Infra.Modules.Goals.Services;

/// <summary>
/// Holds in-memory state for goal progress updates.
/// Used to avoid repeated database queries - instead, event handlers set the flag
/// and the background job checks this flag to know when to recalculate.
/// </summary>
public class GoalProgressState : IGoalProgressState
{
    private volatile bool _hasStaleGoals;
    private volatile bool _bootstrapCompleted;

    /// <summary>
    /// Indicates whether there are stale goals that need recalculation.
    /// </summary>
    public bool HasStaleGoals => _hasStaleGoals;

    /// <summary>
    /// Indicates whether the initial bootstrap run has completed.
    /// </summary>
    public bool BootstrapCompleted => _bootstrapCompleted;

    /// <summary>
    /// Marks that there are stale goals requiring update.
    /// Called by event handlers when transactions or prices change.
    /// </summary>
    public void MarkAsStale()
    {
        _hasStaleGoals = true;
    }

    /// <summary>
    /// Resets the stale flag after goals have been updated.
    /// </summary>
    public void ClearStaleFlag()
    {
        _hasStaleGoals = false;
    }

    /// <summary>
    /// Marks the bootstrap phase as complete.
    /// After this, the job will only check the in-memory flag.
    /// </summary>
    public void MarkBootstrapCompleted()
    {
        _bootstrapCompleted = true;
    }
}
