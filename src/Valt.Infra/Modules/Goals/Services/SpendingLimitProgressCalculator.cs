using System.Text.Json;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class SpendingLimitProgressCalculator : IGoalProgressCalculator
{
    private readonly ILocalDatabase _localDatabase;

    public GoalTypeNames SupportedType => GoalTypeNames.SpendingLimit;

    public SpendingLimitProgressCalculator(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var dto = JsonSerializer.Deserialize<SpendingLimitGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize SpendingLimitGoalType");
        var config = new SpendingLimitGoalType(dto.TargetAmount, dto.Currency, dto.CalculatedSpending);

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        // Get all fiat accounts with the target currency
        // Note: Using AccountEntityTypeId because AccountEntityType is [BsonIgnore]
        var fiatAccountTypeId = (int)AccountEntityType.Fiat;
        var accountsWithCurrency = _localDatabase.GetAccounts()
            .Find(x => x.AccountEntityTypeId == fiatAccountTypeId && x.Currency == config.Currency)
            .Select(x => x.Id)
            .ToHashSet();

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate)
            .ToList();

        // Sum fiat expenses (Fiat type with negative FromFiatAmount from matching currency accounts)
        var fiatExpenses = transactions
            .Where(x => x.Type == TransactionEntityType.Fiat
                        && x.FromFiatAmount < 0
                        && accountsWithCurrency.Contains(x.FromAccountId))
            .Sum(x => Math.Abs(x.FromFiatAmount ?? 0));

        // Sum fiat spent on bitcoin purchases (FiatToBitcoin - FromFiatAmount is negative)
        var bitcoinPurchases = transactions
            .Where(x => x.Type == TransactionEntityType.FiatToBitcoin
                        && x.FromFiatAmount < 0
                        && accountsWithCurrency.Contains(x.FromAccountId))
            .Sum(x => Math.Abs(x.FromFiatAmount ?? 0));

        // Total spending = fiat expenses + bitcoin purchases
        var totalSpending = fiatExpenses + bitcoinPurchases;

        // Calculate percentage (0-100%)
        var progress = config.TargetAmount > 0
            ? Math.Min(100m, Math.Max(0m, (totalSpending * 100m) / config.TargetAmount))
            : 0m;

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSpending(totalSpending);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
