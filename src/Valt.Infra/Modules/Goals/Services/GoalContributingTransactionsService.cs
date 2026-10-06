using LiteDB;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

/// <summary>
/// Resolves the contributing transactions for a goal: loads the goal, derives its period
/// range, delegates row selection to the goal type's calculator (so the summary cannot drift
/// from the progress math), and maps rows to App DTOs with account/category names.
/// </summary>
internal class GoalContributingTransactionsService
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IGoalProgressCalculatorFactory _calculatorFactory;

    public GoalContributingTransactionsService(
        ILocalDatabase localDatabase,
        IGoalProgressCalculatorFactory calculatorFactory)
    {
        _localDatabase = localDatabase;
        _calculatorFactory = calculatorFactory;
    }

    /// <summary>
    /// Returns the contributing transactions for the goal, or null when the goal id is
    /// malformed or the goal does not exist.
    /// </summary>
    public async Task<GoalContributingTransactionsResult?> GetContributingTransactionsAsync(string goalId)
    {
        if (!TryParseObjectId(goalId, out var id))
            return null;

        var entity = _localDatabase.GetGoals().FindById(id);
        if (entity is null)
            return null;

        var typeName = (GoalTypeNames)entity.GoalTypeNameId;
        var refDate = DateOnly.FromDateTime(entity.RefDate);
        var period = (GoalPeriods)entity.PeriodId;
        var startDate = entity.StartDate.HasValue ? DateOnly.FromDateTime(entity.StartDate.Value) : (DateOnly?)null;
        var (from, to) = GoalPeriodRangeHelper.GetRange(refDate, period, startDate);

        var calculator = _calculatorFactory.GetCalculator(typeName);
        var input = new GoalProgressInput(typeName, entity.GoalTypeJson, from, to);

        var rows = await calculator.GetContributingTransactionsAsync(input);
        if (rows is null)
            return new GoalContributingTransactionsResult.NotSupported(typeName);

        var accounts = _localDatabase.GetAccounts().FindAll().ToDictionary(x => x.Id);
        var categories = _localDatabase.GetCategories().FindAll().ToDictionary(x => x.Id.ToString());

        var dtos = rows
            .OrderBy(r => DateOnly.FromDateTime(r.Transaction.Date.ToUniversalTime()))
            .ThenBy(r => r.Transaction.Id)
            .Select(r => MapRow(r, accounts, categories))
            .ToList();

        return new GoalContributingTransactionsResult.Supported(dtos);
    }

    private static bool TryParseObjectId(string value, out ObjectId id)
    {
        id = ObjectId.Empty;

        if (value.Length != 24)
            return false;

        foreach (var c in value)
        {
            var isHex = c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
            if (!isHex)
                return false;
        }

        id = new ObjectId(value);
        return true;
    }

    private static ContributingTransactionRow MapRow(
        GoalContributionRow row,
        Dictionary<ObjectId, AccountEntity> accounts,
        Dictionary<string, CategoryEntity> categories)
    {
        var tx = row.Transaction;

        var accountName = tx.FromAccountId is not null && accounts.TryGetValue(tx.FromAccountId, out var fromAccount)
            ? fromAccount.Name
            : tx.ToAccountId is not null && accounts.TryGetValue(tx.ToAccountId, out var toAccount)
                ? toAccount.Name
                : string.Empty;

        var categoryName = tx.CategoryId is not null && categories.TryGetValue(tx.CategoryId.ToString(), out var category)
            ? category.Name
            : null;

        return new ContributingTransactionRow
        {
            Date = DateOnly.FromDateTime(tx.Date.ToUniversalTime()),
            Description = tx.Name,
            AccountName = accountName,
            CategoryName = categoryName,
            FiatAmount = FiatValue.New(Math.Abs(row.SignedFiatAmount)),
            FiatCurrencyCode = row.FiatCurrencyCode,
            SatsAmount = row.SatsAmount,
            RunningTotal = row.RunningTotal
        };
    }
}
