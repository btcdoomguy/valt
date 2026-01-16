using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

public interface IGoalProgressCalculator
{
    GoalTypeNames SupportedType { get; }
    Task<decimal> CalculateProgressAsync(GoalProgressInput input);
}
