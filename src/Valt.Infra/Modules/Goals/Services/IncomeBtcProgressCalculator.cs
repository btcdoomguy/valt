using System.Text.Json;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
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
        var dto = JsonSerializer.Deserialize<IncomeBtcGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize IncomeBtcGoalType");
        var config = new IncomeBtcGoalType(dto.TargetSats, dto.CalculatedSats);

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
}
