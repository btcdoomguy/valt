using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class BitcoinHodlProgressCalculator : IGoalProgressCalculator
{
    private readonly ILocalDatabase _localDatabase;

    public GoalTypeNames SupportedType => GoalTypeNames.BitcoinHodl;

    public BitcoinHodlProgressCalculator(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeBitcoinHodl(input.GoalTypeJson);

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate)
            .ToList();

        // Sum bitcoin sold (BitcoinToFiat transactions - FromSatAmount is negative when selling)
        var soldSats = transactions
            .Where(x => x.Type == TransactionEntityType.BitcoinToFiat && x.FromSatAmount < 0)
            .Sum(x => Math.Abs(x.FromSatAmount ?? 0));

        // Progress calculation (0-100%): 0% = nothing sold, 100% = at/over limit (failed)
        // - If MaxSellableSats == 0: Progress = soldSats == 0 ? 0 : 100 (full HODL mode - any sale = instant fail)
        // - If MaxSellableSats > 0: Progress = (sold / max) * 100, capped at 100
        decimal progress;
        if (config.MaxSellableSats == 0)
        {
            progress = soldSats == 0 ? 0m : 100m;
        }
        else
        {
            progress = Math.Min(100m, (soldSats * 100m) / config.MaxSellableSats);
        }

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSoldSats(soldSats);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }

    public Task<IReadOnlyList<GoalContributionRow>?> GetContributingTransactionsAsync(GoalProgressInput input)
    {
        // Same range scan and predicate as CalculateProgressAsync (BitcoinToFiat && FromSatAmount < 0) —
        // selection stays beside the progress math, so rows cannot drift from it.
        // Mandatory override: BitcoinHodl is a transaction-derived strategy, so it must never
        // fall back to the interface-default NotSupported.
        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        var mainCurrencyCode = GoalContributingTransactionsCurrency.GetMainFiatCurrencyCode(_localDatabase);
        var accounts = _localDatabase.GetAccounts().FindAll().ToDictionary(x => x.Id);

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
            // Mirror of CalculateProgressAsync's single bucket: BitcoinToFiat && FromSatAmount < 0.
            if (tx.Type != TransactionEntityType.BitcoinToFiat || tx.FromSatAmount is null || tx.FromSatAmount >= 0)
                continue;

            var soldSats = Math.Abs(tx.FromSatAmount.Value);
            var contribution = (decimal)soldSats;
            runningTotal += contribution;
            // BtcValue is a magnitude type (negative sats are rejected), so SatsAmount carries
            // the sold magnitude — the debit nature is expressed by the strategy itself.
            rows.Add(new GoalContributionRow(
                tx,
                tx.FromFiatAmount ?? 0m,
                GoalContributingTransactionsCurrency.ResolveFromAccountCurrency(tx, accounts, mainCurrencyCode),
                BtcValue.ParseSats(soldSats),
                contribution,
                runningTotal));
        }

        return Task.FromResult<IReadOnlyList<GoalContributionRow>?>(rows);
    }
}
