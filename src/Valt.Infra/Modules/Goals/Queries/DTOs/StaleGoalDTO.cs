using Valt.Core.Modules.Goals;

namespace Valt.Infra.Modules.Goals.Queries.DTOs;

public record StaleGoalDTO(
    string Id,
    GoalTypeNames TypeName,
    string GoalTypeJson,
    DateOnly From,
    DateOnly To
);
