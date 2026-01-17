using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Queries;

public interface IGoalQueries
{
    Task<IReadOnlyList<StaleGoalDTO>> GetStaleGoalsAsync();
}
