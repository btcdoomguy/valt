using System.Text.Json;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class IncomeFiatProgressCalculator : IGoalProgressCalculator
{
    private readonly ILocalDatabase _localDatabase;

    public GoalTypeNames SupportedType => GoalTypeNames.IncomeFiat;

    public IncomeFiatProgressCalculator(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var dto = JsonSerializer.Deserialize<IncomeFiatGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize IncomeFiatGoalType");
        var config = new IncomeFiatGoalType(dto.TargetAmount, dto.Currency, dto.CalculatedIncome);

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        // Get all fiat accounts with the target currency
        var fiatAccountTypeId = (int)AccountEntityType.Fiat;
        var accountsWithCurrency = _localDatabase.GetAccounts()
            .Find(x => x.AccountEntityTypeId == fiatAccountTypeId && x.Currency == config.Currency)
            .Select(x => x.Id)
            .ToHashSet();

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate)
            .ToList();

        // Sum fiat income (Fiat type with positive FromFiatAmount from matching currency accounts)
        var fiatIncome = transactions
            .Where(x => x.Type == TransactionEntityType.Fiat
                        && x.FromFiatAmount > 0
                        && accountsWithCurrency.Contains(x.FromAccountId))
            .Sum(x => x.FromFiatAmount ?? 0);

        // Sum fiat received from bitcoin sales (BitcoinToFiat - ToFiatAmount is positive)
        var bitcoinSales = transactions
            .Where(x => x.Type == TransactionEntityType.BitcoinToFiat
                        && x.ToFiatAmount > 0
                        && x.ToAccountId != null
                        && accountsWithCurrency.Contains(x.ToAccountId!))
            .Sum(x => x.ToFiatAmount ?? 0);

        // Total income = fiat income + bitcoin sales
        var totalIncome = fiatIncome + bitcoinSales;

        // Calculate percentage (0-100%)
        var progress = config.TargetAmount > 0
            ? Math.Min(100m, Math.Max(0m, (totalIncome * 100m) / config.TargetAmount))
            : 0m;

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedIncome(totalIncome);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
