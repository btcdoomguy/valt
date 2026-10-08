using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Contracts;

public interface IGoalQueries
{
    Task<IReadOnlyList<StaleGoalDTO>> GetStaleGoalsAsync();
    Task<IReadOnlyList<GoalDTO>> GetGoalsAsync(DateOnly? filterDate);
    Task<GoalDTO?> GetGoalAsync(string goalId);

    /// <summary>
    /// Gets the transactions contributing to a goal's progress. Returns null when the goal
    /// does not exist or the id is malformed; returns GoalContributingTransactionsResult.NotSupported
    /// for goal types whose progress is not transaction-based.
    /// </summary>
    Task<GoalContributingTransactionsResult?> GetContributingTransactionsAsync(string goalId);
}
