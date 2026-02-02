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
}
