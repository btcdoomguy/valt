using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class IncomeFiatProgressCalculator : IGoalProgressCalculator
{
    private readonly IGoalTransactionReader _transactionReader;

    public GoalTypeNames SupportedType => GoalTypeNames.IncomeFiat;

    public IncomeFiatProgressCalculator(IGoalTransactionReader transactionReader)
    {
        _transactionReader = transactionReader;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeIncomeFiat(input.GoalTypeJson);

        // Calculate total income in main fiat currency
        var totalIncome = _transactionReader.CalculateTotalIncome(input.From, input.To);

        // Calculate percentage (0-100%)
        var progress = config.TargetAmount > 0
            ? Math.Min(100m, Math.Max(0m, (totalIncome * 100m) / config.TargetAmount))
            : 0m;

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedIncome(Math.Round(totalIncome, 2));

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
