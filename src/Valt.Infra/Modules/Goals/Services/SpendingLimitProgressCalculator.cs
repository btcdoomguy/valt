using System.Text.Json;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
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
        var dto = JsonSerializer.Deserialize<SpendingLimitGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize SpendingLimitGoalType");
        var config = new SpendingLimitGoalType(dto.TargetAmount, dto.CalculatedSpending);

        // Calculate total expenses in main fiat currency
        var totalSpending = _transactionReader.CalculateTotalExpenses(input.From, input.To);

        // Calculate percentage (0-100%) - inverse: 100% means nothing spent, 0% means at/over limit
        var progress = config.TargetAmount > 0
            ? Math.Max(0m, Math.Min(100m, 100m - ((totalSpending * 100m) / config.TargetAmount)))
            : (totalSpending == 0 ? 100m : 0m);

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSpending(Math.Round(totalSpending, 2));

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
