using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Contracts;

public interface IGoalQueries
{
    Task<IReadOnlyList<StaleGoalDTO>> GetStaleGoalsAsync();
}
