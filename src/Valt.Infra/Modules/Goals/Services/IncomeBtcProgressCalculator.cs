using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class IncomeBtcProgressCalculator : IGoalProgressCalculator
{
    private readonly ILocalDatabase _localDatabase;

    public GoalTypeNames SupportedType => GoalTypeNames.IncomeBtc;

    public IncomeBtcProgressCalculator(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeIncomeBtc(input.GoalTypeJson);

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate)
            .ToList();

        // Sum direct BTC income (Bitcoin transactions with positive FromSatAmount)
        // This includes: bitcoin earned from work, mining rewards, gifts, etc.
        var btcIncome = transactions
            .Where(x => x.Type == TransactionEntityType.Bitcoin && x.FromSatAmount > 0)
            .Sum(x => x.FromSatAmount ?? 0);

        // Calculate percentage (0-100%)
        var progress = config.TargetSats > 0
            ? Math.Min(100m, Math.Max(0m, (btcIncome * 100m) / config.TargetSats))
            : 0m;

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSats(btcIncome);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }

    public Task<IReadOnlyList<GoalContributionRow>?> GetContributingTransactionsAsync(GoalProgressInput input)
    {
        // Same range scan and single bucket predicate as CalculateProgressAsync — selection
        // stays beside the progress math, so rows cannot drift from it.
        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        var mainCurrencyCode = GoalContributingTransactionsCurrency.GetMainFiatCurrencyCode(_localDatabase);

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate)
            .ToList();

        var rows = new List<GoalContributionRow>();
        var runningTotal = 0m;

        var ordered = transactions
            .OrderBy(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
            .ThenBy(x => x.Id);

        foreach (var tx in ordered)
        {
            // Direct BTC income only (Bitcoin transactions with positive FromSatAmount),
            // mirroring CalculateProgressAsync's single bucket.
            if (tx.Type != TransactionEntityType.Bitcoin || tx.FromSatAmount is null || tx.FromSatAmount <= 0)
                continue;

            // Sats-only row: no fiat leg, main currency (Q3 decision); native signed sats.
            var contribution = (decimal)tx.FromSatAmount.Value;
            runningTotal += contribution;
            rows.Add(new GoalContributionRow(
                tx, 0m, mainCurrencyCode, BtcValue.ParseSats(tx.FromSatAmount.Value), contribution, runningTotal));
        }

        return Task.FromResult<IReadOnlyList<GoalContributionRow>?>(rows);
    }
}
