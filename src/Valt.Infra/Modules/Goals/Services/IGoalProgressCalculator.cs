using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

public record GoalProgressResult(decimal Progress, IGoalType UpdatedGoalType);

public interface IGoalProgressCalculator
{
    GoalTypeNames SupportedType { get; }
    Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input);

    /// <summary>
    /// Returns the transactions contributing to the goal for the input period, with the
    /// cumulative running total in the strategy's own unit. Null means the goal type is
    /// not transaction-based (the default; transaction-based calculators override).
    /// Implementations that override must return rows ordered ascending by transaction
    /// date, then transaction id — the mapping service relies on this and does not re-sort.
    /// </summary>
    Task<IReadOnlyList<GoalContributionRow>?> GetContributingTransactionsAsync(GoalProgressInput input) =>
        Task.FromResult<IReadOnlyList<GoalContributionRow>?>(null);
}
