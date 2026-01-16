using Valt.Core.Modules.Goals;

namespace Valt.Infra.Modules.Goals.Queries.DTOs;

public record GoalProgressInput(
    GoalTypeNames TypeName,
    string GoalTypeJson,
    DateOnly From,
    DateOnly To
);
