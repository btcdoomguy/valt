using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class SavingsRateProgressCalculator : IGoalProgressCalculator
{
    private const decimal MaxRateMagnitude = 100m;

    private readonly IGoalTransactionReader _transactionReader;

    public GoalTypeNames SupportedType => GoalTypeNames.SavingsRate;

    public SavingsRateProgressCalculator(IGoalTransactionReader transactionReader)
    {
        _transactionReader = transactionReader;
    }

    public Task<IReadOnlyList<GoalContributionRow>?> GetContributingTransactionsAsync(GoalProgressInput input)
    {
        // Merged income/expense rows (same reader paths as CalculateProgressAsync),
        // with per-row running totals following the incremental percentage formula on
        // unrounded cumulative accumulators — never re-derived from displayed values.
        var merged = _transactionReader.GetIncomeRows(input.From, input.To)
            .Select(r => (Row: r, IsExpense: false))
            .Concat(_transactionReader.GetExpenseRows(input.From, input.To).Select(r => (Row: r, IsExpense: true)))
            .OrderBy(x => DateOnly.FromDateTime(x.Row.Transaction.Date.ToUniversalTime()))
            .ThenBy(x => x.Row.Transaction.Id);

        var rows = new List<GoalContributionRow>();
        var cumulativeIncome = 0m;
        var cumulativeExpenses = 0m;
        foreach (var (row, isExpense) in merged)
        {
            if (isExpense)
                cumulativeExpenses += row.Contribution;
            else
                cumulativeIncome += row.Contribution;

            var runningTotal = cumulativeIncome <= 0
                ? 0m
                : Math.Clamp(
                    Math.Round(((cumulativeIncome - cumulativeExpenses) / cumulativeIncome) * 100m, 2),
                    -MaxRateMagnitude,
                    MaxRateMagnitude);
            rows.Add(row with { RunningTotal = runningTotal });
        }

        return Task.FromResult<IReadOnlyList<GoalContributionRow>?>(rows);
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
            savingsRate = Math.Clamp(savingsRate, -MaxRateMagnitude, MaxRateMagnitude);
        }

        var progress = config.TargetPercentage > 0
            ? Math.Min(100m, Math.Max(0m, (savingsRate * 100m) / config.TargetPercentage))
            : 0m;

        var updatedGoalType = config.WithCalculatedPercentage(savingsRate);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
