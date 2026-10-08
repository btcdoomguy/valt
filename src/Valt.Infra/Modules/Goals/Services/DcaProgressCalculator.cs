using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class DcaProgressCalculator : IGoalProgressCalculator
{
    private readonly ILocalDatabase _localDatabase;

    public GoalTypeNames SupportedType => GoalTypeNames.Dca;

    public DcaProgressCalculator(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeDca(input.GoalTypeJson);

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        // Count FiatToBitcoin transactions (bitcoin purchases)
        var purchaseCount = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate && x.Type == TransactionEntityType.FiatToBitcoin)
            .Count();

        // Calculate percentage (0-100%)
        var progress = config.TargetPurchaseCount > 0
            ? Math.Min(100m, Math.Max(0m, (purchaseCount * 100m) / config.TargetPurchaseCount))
            : 0m;

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedPurchaseCount(purchaseCount);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }

    public Task<IReadOnlyList<GoalContributionRow>?> GetContributingTransactionsAsync(GoalProgressInput input)
    {
        // Same range scan and predicate as CalculateProgressAsync (FiatToBitcoin only) —
        // selection stays beside the progress math, so rows cannot drift from it.
        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        var mainCurrencyCode = GoalContributingTransactionsCurrency.GetMainFiatCurrencyCode(_localDatabase);
        var accounts = _localDatabase.GetAccounts().FindAll().ToDictionary(x => x.Id);

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate && x.Type == TransactionEntityType.FiatToBitcoin)
            .ToList();

        var rows = new List<GoalContributionRow>();
        var runningTotal = 0m;

        var ordered = transactions
            .OrderBy(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
            .ThenBy(x => x.Id);

        foreach (var tx in ordered)
        {
            // Progress is a purchase count: each row contributes 1 (Q2 count-unit decision).
            var contribution = 1m;
            runningTotal += contribution;
            rows.Add(new GoalContributionRow(
                tx,
                tx.FromFiatAmount ?? 0m,
                GoalContributingTransactionsCurrency.ResolveFromAccountCurrency(tx, accounts, mainCurrencyCode),
                BtcValue.ParseSats(Math.Abs(tx.ToSatAmount ?? 0)),
                contribution,
                runningTotal));
        }

        return Task.FromResult<IReadOnlyList<GoalContributionRow>?>(rows);
    }
}
