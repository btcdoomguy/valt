using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class SaveFiatProgressCalculator : IGoalProgressCalculator
{
    private readonly IGoalTransactionReader _transactionReader;

    public GoalTypeNames SupportedType => GoalTypeNames.SaveFiat;

    public SaveFiatProgressCalculator(IGoalTransactionReader transactionReader)
    {
        _transactionReader = transactionReader;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeSaveFiat(input.GoalTypeJson);

        var totalIncome = _transactionReader.CalculateTotalIncome(input.From, input.To);
        var totalExpenses = _transactionReader.CalculateTotalExpenses(input.From, input.To);
        var savings = totalIncome - totalExpenses;

        var progress = config.TargetAmount > 0
            ? Math.Min(100m, Math.Max(0m, (savings * 100m) / config.TargetAmount))
            : 0m;

        var updatedGoalType = config.WithCalculatedSavings(Math.Round(savings, 2));

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
