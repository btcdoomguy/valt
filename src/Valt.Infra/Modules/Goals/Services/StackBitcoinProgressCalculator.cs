using System.Text.Json;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
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

    public Task<decimal> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = JsonSerializer.Deserialize<StackBitcoinGoalType>(input.GoalTypeJson)
                     ?? throw new InvalidOperationException("Failed to deserialize StackBitcoinGoalType");

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        // Sum all BTC added (positive sat amounts where ToSatAmount > 0)
        var btcAdded = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate && x.ToSatAmount > 0)
            .Sum(x => x.ToSatAmount ?? 0);

        // Calculate percentage
        var progress = config.TargetSats > 0
            ? Math.Min(100m, (btcAdded * 100m) / config.TargetSats)
            : 0m;

        return Task.FromResult(progress);
    }
}
