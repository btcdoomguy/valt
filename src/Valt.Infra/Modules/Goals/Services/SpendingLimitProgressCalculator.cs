using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class SpendingLimitProgressCalculator : IGoalProgressCalculator
{
    private readonly IGoalTransactionReader _transactionReader;

    public GoalTypeNames SupportedType => GoalTypeNames.SpendingLimit;

    public SpendingLimitProgressCalculator(IGoalTransactionReader transactionReader)
    {
        _transactionReader = transactionReader;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeSpendingLimit(input.GoalTypeJson);

        // Calculate total expenses in main fiat currency
        var totalSpending = _transactionReader.CalculateTotalExpenses(input.From, input.To);

        // Calculate percentage (0-100%): 0% = nothing spent, 100% = at/over limit (failed)
        var progress = config.TargetAmount > 0
            ? Math.Min(100m, (totalSpending * 100m) / config.TargetAmount)
            : (totalSpending == 0 ? 0m : 100m);

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSpending(Math.Round(totalSpending, 2));

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
