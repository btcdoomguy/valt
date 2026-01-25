namespace Valt.App.Modules.Goals.DTOs;

public record StaleGoalDTO(
    string Id,
    int TypeId,
    string GoalTypeJson,
    DateOnly From,
    DateOnly To
);
