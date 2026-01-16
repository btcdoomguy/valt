using System.Text.Json;
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
        var dto = JsonSerializer.Deserialize<StackBitcoinGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize StackBitcoinGoalType");
        var config = new StackBitcoinGoalType(dto.TargetSats, dto.CalculatedSats);

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
}
