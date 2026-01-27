using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetStaleGoals;

public record GetStaleGoalsQuery : IQuery<IReadOnlyList<StaleGoalDTO>>;
