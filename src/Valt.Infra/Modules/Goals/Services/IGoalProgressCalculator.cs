using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

public record GoalProgressResult(decimal Progress, IGoalType UpdatedGoalType);

public interface IGoalProgressCalculator
{
    GoalTypeNames SupportedType { get; }
    Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input);
}
