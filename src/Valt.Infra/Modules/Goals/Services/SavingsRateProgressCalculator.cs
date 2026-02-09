using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class SavingsRateProgressCalculator : IGoalProgressCalculator
{
    private readonly IGoalTransactionReader _transactionReader;

    public GoalTypeNames SupportedType => GoalTypeNames.SavingsRate;

    public SavingsRateProgressCalculator(IGoalTransactionReader transactionReader)
    {
        _transactionReader = transactionReader;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeSavingsRate(input.GoalTypeJson);

        var totalIncome = _transactionReader.CalculateTotalIncome(input.From, input.To);
        var totalExpenses = _transactionReader.CalculateTotalExpenses(input.From, input.To);

        decimal savingsRate;
        if (totalIncome <= 0)
        {
            savingsRate = 0;
        }
        else
        {
            var savings = totalIncome - totalExpenses;
            savingsRate = Math.Round((savings / totalIncome) * 100m, 2);
        }

        var progress = config.TargetPercentage > 0
            ? Math.Min(100m, Math.Max(0m, (savingsRate * 100m) / config.TargetPercentage))
            : 0m;

        var updatedGoalType = config.WithCalculatedPercentage(savingsRate);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
