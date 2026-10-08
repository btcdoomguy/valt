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

    public Task<IReadOnlyList<GoalContributionRow>?> GetContributingTransactionsAsync(GoalProgressInput input)
    {
        // Merge income and expense rows from the reader (the only selection paths,
        // so rows cannot drift from CalculateProgressAsync), then re-accumulate the
        // running total as cumulative income minus cumulative expenses.
        var merged = _transactionReader.GetIncomeRows(input.From, input.To)
            .Select(r => (Row: r, IsExpense: false))
            .Concat(_transactionReader.GetExpenseRows(input.From, input.To).Select(r => (Row: r, IsExpense: true)))
            .OrderBy(x => DateOnly.FromDateTime(x.Row.Transaction.Date.ToUniversalTime()))
            .ThenBy(x => x.Row.Transaction.Id);

        var rows = new List<GoalContributionRow>();
        var runningTotal = 0m;
        foreach (var (row, isExpense) in merged)
        {
            runningTotal += isExpense ? -row.Contribution : row.Contribution;
            rows.Add(row with { RunningTotal = runningTotal });
        }

        return Task.FromResult<IReadOnlyList<GoalContributionRow>?>(rows);
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
