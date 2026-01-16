using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.Goals.Contracts;

public interface IGoalRepository : IRepository
{
    Task<Goal?> GetByIdAsync(GoalId id);
    Task SaveAsync(Goal goal);
    Task<IEnumerable<Goal>> GetAllAsync();
    Task DeleteAsync(Goal goal);
    Task MarkGoalsStaleForDateAsync(DateOnly date);
}
