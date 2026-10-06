using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Goals.Queries;

[TestFixture]
public class GetGoalContributingTransactionsHandlerTests : DatabaseTest
{
    private GetGoalContributingTransactionsHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        var existingGoals = await _goalRepository.GetAllAsync();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetAccounts().DeleteAll();
        _localDatabase.GetCategories().DeleteAll();

        _handler = new GetGoalContributingTransactionsHandler(_goalQueries);
    }

    [Test]
    public async Task SpendingLimit_Reconciles()
    {
        // Arrange
        var goal = GoalBuilder.ASpendingLimitGoal(1000m)
            .WithRefDate(new DateOnly(2024, 6, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var account = FiatAccountBuilder.AnAccount()
            .WithName("Checking")
            .WithFiatCurrency(FiatCurrency.Usd)
            .Build();
        _localDatabase.GetAccounts().Insert(account);

        PriceDataBuilder.SeedRange(_priceDatabase,
            new DateTime(2024, 5, 24), new DateTime(2024, 7, 7), 50_000m);

        var transaction = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 15))
            .WithName("Grocery")
            .AsFiatExpense(new Valt.Core.Modules.Budget.Accounts.AccountId(account.Id.ToString()), 300m)
            .Build();
        _localDatabase.GetTransactions().Insert(transaction);

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new SpendingLimitProgressCalculator(
            new GoalTransactionReader(_localDatabase, _priceDatabase,
                new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())));
        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            entity.GoalTypeJson,
            new DateOnly(2024, 6, 1),
            new DateOnly(2024, 6, 30));
        var progress = await calculator.CalculateProgressAsync(input);
        var calculatedSpending = ((SpendingLimitGoalType)progress.UpdatedGoalType).CalculatedSpending;

        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goal.Id.Value });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var supported = (GoalContributingTransactionsResult.Supported)result.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(supported.Rows, Has.Count.EqualTo(1));
            var row = supported.Rows[0];
            Assert.That(row.Date, Is.EqualTo(new DateOnly(2024, 6, 15)));
            Assert.That(row.Description, Is.EqualTo("Grocery"));
            Assert.That(row.AccountName, Is.EqualTo("Checking"));
            Assert.That(row.CategoryName, Is.Null);
            Assert.That(row.FiatAmount.Value, Is.EqualTo(300m));
            Assert.That(row.FiatCurrencyCode, Is.EqualTo("USD"));
            Assert.That(row.SatsAmount.Sats, Is.EqualTo(600_000));
            Assert.That(row.RunningTotal, Is.EqualTo(300m));
            Assert.That(row.RunningTotal, Is.EqualTo(calculatedSpending));
        });
    }

    [Test]
    public async Task NetWorthBtc_ReturnsNotSupported()
    {
        // Arrange
        var goal = GoalBuilder.ANetWorthBtcGoal()
            .WithRefDate(new DateOnly(2024, 6, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);

        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goal.Id.Value });

        // Assert: comes from the interface default — no NetWorthBtc calculator code
        Assert.That(result.IsSuccess, Is.True);
        var notSupported = (GoalContributingTransactionsResult.NotSupported)result.Value!;
        Assert.That(notSupported.Type, Is.EqualTo(GoalTypeNames.NetWorthBtc));
    }

    [Test]
    public async Task MissingGoalId_ReturnsGoalNotFound()
    {
        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = "000000000000000000000099" });

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error!.Code, Is.EqualTo("GOAL_NOT_FOUND"));
    }

    [Test]
    public async Task MalformedGoalId_ReturnsGoalNotFound()
    {
        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = "not-an-object-id" });

        // Assert: defensive parse path — no exception
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error!.Code, Is.EqualTo("GOAL_NOT_FOUND"));
    }

    [Test]
    public async Task NoTransactions_ReturnsSupportedEmptyList()
    {
        // Arrange
        var goalId = await SeedSpendingLimitGoal(new DateOnly(2024, 6, 15));
        SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goalId });

        // Assert: empty contributing set is Supported with an empty list, never a failure
        Assert.That(result.IsSuccess, Is.True);
        var supported = (GoalContributingTransactionsResult.Supported)result.Value!;
        Assert.That(supported.Rows, Is.Empty);
    }

    [Test]
    public async Task AddRemove_ChangesSetAndFinalTotal()
    {
        // Arrange
        var goalId = await SeedSpendingLimitGoal(new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedExpense(account, 300m, new DateOnly(2024, 6, 10));

        var first = await Dispatch(goalId);
        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first[0].RunningTotal, Is.EqualTo(300m));

        // Act: add a second expense
        var secondTx = SeedExpense(account, 200m, new DateOnly(2024, 6, 20));

        var afterAdd = await Dispatch(goalId);
        Assert.That(afterAdd, Has.Count.EqualTo(2));
        Assert.That(afterAdd[0].Date, Is.LessThan(afterAdd[1].Date));
        Assert.That(afterAdd[1].RunningTotal, Is.EqualTo(500m));

        // Act: remove the second expense
        _localDatabase.GetTransactions().Delete(secondTx.Id);

        var afterRemove = await Dispatch(goalId);
        Assert.That(afterRemove, Has.Count.EqualTo(1));
        Assert.That(afterRemove[0].RunningTotal, Is.EqualTo(300m));
    }

    [Test]
    public async Task Transfers_NeverAppear()
    {
        // Arrange
        var goalId = await SeedSpendingLimitGoal(new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedExpense(account, 300m, new DateOnly(2024, 6, 15), "Real expense");

        var purchase = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10))
            .WithName("Bitcoin purchase")
            .AsBitcoinPurchase(satAmount: 100_000, fiatAmount: 100m)
            .Build();
        _localDatabase.GetTransactions().Insert(purchase);

        var sale = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 20))
            .WithName("Bitcoin sale")
            .AsBitcoinSale(satAmount: 100_000, fiatAmount: 100m)
            .Build();
        _localDatabase.GetTransactions().Insert(sale);

        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goalId });

        // Assert: only the fiat expense row is returned — transfer exclusion is structural
        Assert.That(result.IsSuccess, Is.True);
        var supported = (GoalContributingTransactionsResult.Supported)result.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(supported.Rows, Has.Count.EqualTo(1));
            Assert.That(supported.Rows[0].Description, Is.EqualTo("Real expense"));
            Assert.That(supported.Rows[0].RunningTotal, Is.EqualTo(300m));
        });
    }

    [Test]
    public async Task Rows_Ascending_WithStableSameDayOrder()
    {
        // Arrange
        var goalId = await SeedSpendingLimitGoal(new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedExpense(account, 300m, new DateOnly(2024, 6, 5));
        SeedExpense(account, 200m, new DateOnly(2024, 6, 10));
        SeedExpense(account, 200m, new DateOnly(2024, 6, 15));
        SeedExpense(account, 200m, new DateOnly(2024, 6, 20), "First same-day");
        var sameDaySecond = SeedExpense(account, 200m, new DateOnly(2024, 6, 20), "Second same-day");

        // Act
        var rows = await Dispatch(goalId);

        // Assert: date ascending; same-day rows follow Transaction.Id order with deterministic running totals
        Assert.That(rows, Has.Count.EqualTo(5));
        Assert.Multiple(() =>
        {
            for (var i = 1; i < rows.Count; i++)
                Assert.That(rows[i].Date, Is.GreaterThanOrEqualTo(rows[i - 1].Date));

            Assert.That(rows[3].Date, Is.EqualTo(rows[4].Date));
            Assert.That(rows[3].Description, Is.Not.EqualTo(rows[4].Description));
            Assert.That(rows[4].Description, Is.EqualTo(sameDaySecond.Name));
            Assert.That(rows.Select(r => r.RunningTotal), Is.EqualTo(new[] { 300m, 500m, 700m, 900m, 1100m }));
        });
    }

    [Test]
    public async Task BoundaryDates_Included_OneDayOutside_Excluded()
    {
        // Arrange: monthly June goal — 1st and 30th inside, May 31 and July 1 outside
        var goalId = await SeedSpendingLimitGoal(new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedExpense(account, 100m, new DateOnly(2024, 5, 31), "Before period");
        SeedExpense(account, 100m, new DateOnly(2024, 6, 1), "First day");
        SeedExpense(account, 200m, new DateOnly(2024, 6, 30), "Last day");
        SeedExpense(account, 100m, new DateOnly(2024, 7, 1), "After period");

        // Act
        var rows = await Dispatch(goalId);

        // Assert: exactly the two boundary-dated expenses, final total their sum
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Description, Is.EqualTo("First day"));
            Assert.That(rows[1].Description, Is.EqualTo("Last day"));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(300m));
        });
    }

    [Test]
    public async Task ForeignCurrencyAccount_Converts_AtTxDateRate()
    {
        // Arrange: BRL account, seeded BRL rate 5.0 (BRL per USD)
        var goalId = await SeedSpendingLimitGoal(new DateOnly(2024, 6, 15));
        var account = FiatAccountBuilder.AnAccount()
            .WithName("BRL Checking")
            .WithFiatCurrency(FiatCurrency.Brl)
            .Build();
        _localDatabase.GetAccounts().Insert(account);

        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m, ("BRL", 5.0m));

        SeedExpense(account, 1000m, new DateOnly(2024, 6, 15), "BRL expense");

        // Act
        var rows = await Dispatch(goalId);

        // Assert: natural original-currency amount on the row; strategy-unit total in main currency
        Assert.That(rows, Has.Count.EqualTo(1));
        var row = rows[0];
        Assert.Multiple(() =>
        {
            Assert.That(row.FiatAmount.Value, Is.EqualTo(1000m));
            Assert.That(row.FiatCurrencyCode, Is.EqualTo("BRL"));
            Assert.That(row.RunningTotal, Is.EqualTo(200m));
        });
    }

    [Test]
    public async Task Sats_Converted_AtTransactionDatePrice_NotLive()
    {
        // Arrange: BTC price seeded per-day — 40k through Jun 15, 60k from Jun 16
        var goalId = await SeedSpendingLimitGoal(new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 6, 15), 40_000m);
        SeedPrices(new DateOnly(2024, 6, 16), new DateOnly(2024, 7, 7), 60_000m);

        SeedExpense(account, 100m, new DateOnly(2024, 6, 10), "At 40k day");
        SeedExpense(account, 100m, new DateOnly(2024, 6, 20), "At 60k day");

        // Act
        var rows = await Dispatch(goalId);

        // Assert: each row's sats match its own date's seeded price, proving per-row tx-date conversion
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].SatsAmount.Sats, Is.EqualTo(250_000L)); // 100 USD / 40_000
            Assert.That(rows[1].SatsAmount.Sats, Is.EqualTo(Convert.ToInt64(100m / 60_000m * 100_000_000m)));
            Assert.That(rows[0].SatsAmount.Sats, Is.Not.EqualTo(rows[1].SatsAmount.Sats));
        });
    }

    private async Task<string> SeedSpendingLimitGoal(DateOnly refDate)
    {
        var goal = GoalBuilder.ASpendingLimitGoal(1000m)
            .WithRefDate(refDate)
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);
        return goal.Id.Value;
    }

    private AccountEntity SeedUsdAccount(string name = "Checking")
    {
        var account = FiatAccountBuilder.AnAccount()
            .WithName(name)
            .WithFiatCurrency(FiatCurrency.Usd)
            .Build();
        _localDatabase.GetAccounts().Insert(account);
        return account;
    }

    private void SeedPrices(DateOnly from, DateOnly to, decimal btcPriceUsd, params (string CurrencyCode, decimal Price)[] fiatRates)
    {
        PriceDataBuilder.SeedRange(_priceDatabase,
            from.ToDateTime(TimeOnly.MinValue),
            to.ToDateTime(TimeOnly.MinValue),
            btcPriceUsd,
            fiatRates);
    }

    private TransactionEntity SeedExpense(AccountEntity account, decimal amount, DateOnly date, string name = "Expense")
    {
        var transaction = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName(name)
            .AsFiatExpense(new AccountId(account.Id.ToString()), amount)
            .Build();
        _localDatabase.GetTransactions().Insert(transaction);
        return transaction;
    }

    private async Task<IReadOnlyList<ContributingTransactionRow>> Dispatch(string goalId)
    {
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goalId });
        Assert.That(result.IsSuccess, Is.True);
        return ((GoalContributingTransactionsResult.Supported)result.Value!).Rows;
    }
}
