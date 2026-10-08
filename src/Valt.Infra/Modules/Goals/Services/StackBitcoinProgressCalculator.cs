using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class StackBitcoinProgressCalculator : IGoalProgressCalculator
{
    private readonly ILocalDatabase _localDatabase;

    public GoalTypeNames SupportedType => GoalTypeNames.StackBitcoin;

    public StackBitcoinProgressCalculator(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeStackBitcoin(input.GoalTypeJson);

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate)
            .ToList();

        // Sum BTC purchased (FiatToBitcoin transactions)
        var btcPurchased = transactions
            .Where(x => x.Type == TransactionEntityType.FiatToBitcoin && x.ToSatAmount > 0)
            .Sum(x => x.ToSatAmount ?? 0);

        // Sum direct BTC income (Bitcoin transactions with positive FromSatAmount)
        var btcIncome = transactions
            .Where(x => x.Type == TransactionEntityType.Bitcoin && x.FromSatAmount > 0)
            .Sum(x => x.FromSatAmount ?? 0);

        // Sum BTC sold (BitcoinToFiat transactions - FromSatAmount is negative)
        var btcSold = transactions
            .Where(x => x.Type == TransactionEntityType.BitcoinToFiat && x.FromSatAmount < 0)
            .Sum(x => Math.Abs(x.FromSatAmount ?? 0));

        // Sum direct BTC expenses (Bitcoin transactions with negative FromSatAmount)
        var btcExpenses = transactions
            .Where(x => x.Type == TransactionEntityType.Bitcoin && x.FromSatAmount < 0)
            .Sum(x => Math.Abs(x.FromSatAmount ?? 0));

        // Net stacked = (Purchased + Income) - (Sold + Expenses)
        var netBtcStacked = (btcPurchased + btcIncome) - (btcSold + btcExpenses);

        // Calculate percentage (allow negative progress if user sold more than purchased)
        var progress = config.TargetSats > 0
            ? Math.Min(100m, Math.Max(0m, (netBtcStacked * 100m) / config.TargetSats))
            : 0m;

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSats(netBtcStacked);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }

    public Task<IReadOnlyList<GoalContributionRow>?> GetContributingTransactionsAsync(GoalProgressInput input)
    {
        // Same range scan as CalculateProgressAsync with the four bucket predicates mirrored
        // verbatim — selection stays beside the progress math, so rows cannot drift from it.
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
            long? signedSats = tx.Type switch
            {
                // BTC purchased (FiatToBitcoin transactions)
                TransactionEntityType.FiatToBitcoin when tx.ToSatAmount > 0 => tx.ToSatAmount.Value,
                // Direct BTC income (Bitcoin transactions with positive FromSatAmount)
                TransactionEntityType.Bitcoin when tx.FromSatAmount > 0 => tx.FromSatAmount.Value,
                // BTC sold (BitcoinToFiat transactions - FromSatAmount is negative)
                TransactionEntityType.BitcoinToFiat when tx.FromSatAmount < 0 => -Math.Abs(tx.FromSatAmount.Value),
                // Direct BTC expenses (Bitcoin transactions with negative FromSatAmount)
                TransactionEntityType.Bitcoin when tx.FromSatAmount < 0 => -Math.Abs(tx.FromSatAmount.Value),
                _ => null,
            };
            if (signedSats is null)
                continue;

            // Transfers with a fiat leg carry it: purchases (FiatToBitcoin) persist the fiat
            // leg on the "from" side, sales (BitcoinToFiat) on the "to" side (their
            // FromFiatAmount is always null) — resolve currency against the fiat-leg account.
            // Direct-Bitcoin rows are sats-only: zero fiat, main currency (Q3 decision).
            var hasFiatLeg = tx.Type is TransactionEntityType.FiatToBitcoin or TransactionEntityType.BitcoinToFiat;
            var isSale = tx.Type == TransactionEntityType.BitcoinToFiat;
            var fiatAccountId = isSale ? tx.ToAccountId : tx.FromAccountId;
            var signedFiatAmount = hasFiatLeg
                ? (isSale ? tx.ToFiatAmount ?? 0m : tx.FromFiatAmount ?? 0m)
                : 0m;
            var fiatCurrencyCode = hasFiatLeg
                ? GoalContributingTransactionsCurrency.ResolveAccountCurrency(fiatAccountId, accounts, mainCurrencyCode)
                : mainCurrencyCode;

            var contribution = (decimal)signedSats.Value;
            runningTotal += contribution;
            // BtcValue is a magnitude type (negative sats are rejected), so SatsAmount carries
            // the absolute sats — the natural sign lives on Contribution/RunningTotal.
            rows.Add(new GoalContributionRow(
                tx, signedFiatAmount, fiatCurrencyCode, BtcValue.ParseSats(Math.Abs(signedSats.Value)), contribution, runningTotal));
        }

        return Task.FromResult<IReadOnlyList<GoalContributionRow>?>(rows);
    }
}
