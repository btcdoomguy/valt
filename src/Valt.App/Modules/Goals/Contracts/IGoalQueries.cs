using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Contracts;

public interface IGoalQueries
{
    Task<IReadOnlyList<StaleGoalDTO>> GetStaleGoalsAsync();
    Task<IReadOnlyList<GoalDTO>> GetGoalsAsync(DateOnly? filterDate);
    Task<GoalDTO?> GetGoalAsync(string goalId);
}
