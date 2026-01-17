using System.Text.Json;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
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
        var dto = JsonSerializer.Deserialize<BitcoinHodlGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize BitcoinHodlGoalType");
        var config = new BitcoinHodlGoalType(dto.MaxSellableSats, dto.CalculatedSoldSats);

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
}
