using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
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
    public async Task SatsOnlyRows_CarryZeroFiat_AndMainCurrency()
    {
        // Arrange: direct-Bitcoin income and expense rows (no fiat leg anywhere)
        var goalId = await SeedGoal(GoalBuilder.AStackBitcoinGoal(1_000_000), new DateOnly(2024, 6, 15));
        var btcAccount = SeedBtcAccount();
        var btcAccountId = new AccountId(btcAccount.Id.ToString());

        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10)).WithName("BTC income")
            .AsBitcoinIncome(btcAccountId, 100_000));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 15)).WithName("BTC spend")
            .AsBitcoinExpense(btcAccountId, 25_000));

        // Act
        var rows = await Dispatch(goalId);

        // Assert: Q3 contract — zero fiat + main currency on sats-only rows; sign on RunningTotal
        Assert.That(rows, Has.Count.EqualTo(2));
        var incomeRow = rows.Single(r => r.Description == "BTC income");
        var expenseRow = rows.Single(r => r.Description == "BTC spend");
        Assert.Multiple(() =>
        {
            Assert.That(incomeRow.FiatAmount.Value, Is.EqualTo(0m));
            Assert.That(incomeRow.FiatCurrencyCode, Is.EqualTo("USD"));
            Assert.That(incomeRow.SatsAmount.Sats, Is.EqualTo(100_000));
            Assert.That(incomeRow.RunningTotal, Is.EqualTo(100_000m));

            Assert.That(expenseRow.FiatAmount.Value, Is.EqualTo(0m));
            Assert.That(expenseRow.FiatCurrencyCode, Is.EqualTo("USD"));
            Assert.That(expenseRow.SatsAmount.Sats, Is.EqualTo(25_000));
            Assert.That(expenseRow.RunningTotal, Is.EqualTo(75_000m));
        });
    }

    [Test]
    public async Task SameDayRows_StableAcrossStrategies()
    {
        // Arrange: StackBitcoin same-day bitcoin incomes
        var stackGoalId = await SeedGoal(GoalBuilder.AStackBitcoinGoal(1_000_000), new DateOnly(2024, 6, 15));
        var btcAccount = SeedBtcAccount();
        var btcAccountId = new AccountId(btcAccount.Id.ToString());
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 20)).WithName("First same-day")
            .AsBitcoinIncome(btcAccountId, 100_000));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 20)).WithName("Second same-day")
            .AsBitcoinIncome(btcAccountId, 50_000));

        // Arrange: SaveFiat same-day expense and income
        var saveGoalId = await SeedGoal(GoalBuilder.ASaveFiatGoal(1000m), new DateOnly(2024, 6, 15));
        var fiatAccount = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);
        SeedExpense(fiatAccount, 300m, new DateOnly(2024, 6, 20), "Expense same-day");
        SeedIncome(fiatAccount, 500m, new DateOnly(2024, 6, 20), "Income same-day");

        // Act: each strategy dispatched twice — order and totals must be identical
        var stackFirst = await Dispatch(stackGoalId);
        var stackSecond = await Dispatch(stackGoalId);
        var saveFirst = await Dispatch(saveGoalId);
        var saveSecond = await Dispatch(saveGoalId);

        // Assert: deterministic (Date, Id) order with matching running-total sequences
        Assert.Multiple(() =>
        {
            Assert.That(stackFirst.Select(r => r.Description),
                Is.EqualTo(new[] { "First same-day", "Second same-day" }));
            Assert.That(stackFirst.Select(r => r.RunningTotal), Is.EqualTo(new[] { 100_000m, 150_000m }));
            Assert.That(stackSecond.Select(r => r.Description), Is.EqualTo(stackFirst.Select(r => r.Description)));
            Assert.That(stackSecond.Select(r => r.RunningTotal), Is.EqualTo(stackFirst.Select(r => r.RunningTotal)));

            Assert.That(saveFirst.Select(r => r.Description),
                Is.EqualTo(new[] { "Expense same-day", "Income same-day" }));
            Assert.That(saveFirst.Select(r => r.RunningTotal), Is.EqualTo(new[] { -300m, 200m }));
            Assert.That(saveSecond.Select(r => r.Description), Is.EqualTo(saveFirst.Select(r => r.Description)));
            Assert.That(saveSecond.Select(r => r.RunningTotal), Is.EqualTo(saveFirst.Select(r => r.RunningTotal)));
        });
    }

    [Test]
    public async Task AddRemove_DirectDb()
    {
        // Arrange: StackBitcoin baseline purchase
        var goalId = await SeedGoal(GoalBuilder.AStackBitcoinGoal(1_000_000), new DateOnly(2024, 6, 15));
        var btcAccount = SeedBtcAccount();
        var fiatAccount = SeedUsdAccount();
        var btcAccountId = new AccountId(btcAccount.Id.ToString());
        var fiatAccountId = new AccountId(fiatAccount.Id.ToString());

        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10)).WithName("Baseline purchase")
            .AsBitcoinPurchase(fiatAccountId, btcAccountId, 100_000, 500m));

        var first = await Dispatch(goalId);
        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first[0].RunningTotal, Is.EqualTo(100_000m));

        // Act: add an earlier purchase — must land in chronological position and move the total
        var extraPurchase = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 5)).WithName("Earlier purchase")
            .AsBitcoinPurchase(fiatAccountId, btcAccountId, 50_000, 250m)
            .Build();
        _localDatabase.GetTransactions().Insert(extraPurchase);

        var afterAdd = await Dispatch(goalId);
        Assert.That(afterAdd, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(afterAdd[0].Description, Is.EqualTo("Earlier purchase"));
            Assert.That(afterAdd[0].Date, Is.LessThan(afterAdd[1].Date));
            Assert.That(afterAdd[1].RunningTotal, Is.EqualTo(150_000m));
        });

        // Act: remove it — set and total revert exactly
        _localDatabase.GetTransactions().Delete(extraPurchase.Id);

        var afterRemove = await Dispatch(goalId);
        Assert.That(afterRemove, Has.Count.EqualTo(1));
        Assert.That(afterRemove[0].RunningTotal, Is.EqualTo(100_000m));
    }

    [Test]
    public async Task Dca_CountRunningTotal_Reconciles()
    {
        // Arrange: three FiatToBitcoin purchases in period
        var goalId = await SeedGoal(
            GoalBuilder.AGoal().WithGoalType(new DcaGoalType(10)), new DateOnly(2024, 6, 15));
        var btcAccount = SeedBtcAccount();
        var fiatAccount = SeedUsdAccount();
        var btcAccountId = new AccountId(btcAccount.Id.ToString());
        var fiatAccountId = new AccountId(fiatAccount.Id.ToString());

        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 5)).WithName("Purchase 1")
            .AsBitcoinPurchase(fiatAccountId, btcAccountId, 50_000, 250m));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 12)).WithName("Purchase 2")
            .AsBitcoinPurchase(fiatAccountId, btcAccountId, 60_000, 300m));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 25)).WithName("Purchase 3")
            .AsBitcoinPurchase(fiatAccountId, btcAccountId, 70_000, 350m));

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new DcaProgressCalculator(_localDatabase);
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.Dca, entity.GoalTypeJson));
        var calculatedPurchaseCount = ((DcaGoalType)progress.UpdatedGoalType).CalculatedPurchaseCount;

        // Act
        var rows = await Dispatch(goalId);

        // Assert: one row per purchase; RunningTotal is the cumulative count (Q2 count unit)
        Assert.That(rows, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(r => r.Description),
                Is.EqualTo(new[] { "Purchase 1", "Purchase 2", "Purchase 3" }));
            Assert.That(rows.Select(r => r.RunningTotal), Is.EqualTo(new[] { 1m, 2m, 3m }));
            Assert.That(rows[2].RunningTotal, Is.EqualTo(calculatedPurchaseCount));
        });
    }

    [Test]
    public async Task BitcoinHodl_SoldSats_Reconciles_AndIsSupported()
    {
        // Arrange: one BitcoinToFiat sale in period
        var goalId = await SeedGoal(GoalBuilder.ABitcoinHodlGoal(100_000), new DateOnly(2024, 6, 15));
        var btcAccount = SeedBtcAccount();
        var fiatAccount = SeedUsdAccount();
        var btcAccountId = new AccountId(btcAccount.Id.ToString());
        var fiatAccountId = new AccountId(fiatAccount.Id.ToString());

        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10)).WithName("Sold some")
            .AsBitcoinSale(btcAccountId, fiatAccountId, 40_000, 200m));

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new BitcoinHodlProgressCalculator(_localDatabase);
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.BitcoinHodl, entity.GoalTypeJson));
        var calculatedSoldSats = ((BitcoinHodlGoalType)progress.UpdatedGoalType).CalculatedSoldSats;

        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goalId });

        // Assert: Supported (never NotSupported — ninth transaction-based type), sold sats reconcile
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.TypeOf<GoalContributingTransactionsResult.Supported>());
        var rows = ((GoalContributingTransactionsResult.Supported)result.Value!).Rows;
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Description, Is.EqualTo("Sold some"));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(40_000m));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(calculatedSoldSats));
            Assert.That(rows[0].SatsAmount.Sats, Is.EqualTo(40_000));
        });
    }

    [Test]
    public async Task StackBitcoin_FourBuckets_NetSats_Reconcile()
    {
        // Arrange: one transaction of each StackBitcoin bucket in period
        var goalId = await SeedGoal(GoalBuilder.AStackBitcoinGoal(1_000_000), new DateOnly(2024, 6, 15));
        var btcAccount = SeedBtcAccount();
        var fiatAccount = SeedUsdAccount();
        var btcAccountId = new AccountId(btcAccount.Id.ToString());
        var fiatAccountId = new AccountId(fiatAccount.Id.ToString());

        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 5)).WithName("Purchase")
            .AsBitcoinPurchase(fiatAccountId, btcAccountId, 100_000, 500m));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10)).WithName("BTC income")
            .AsBitcoinIncome(btcAccountId, 50_000));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 15)).WithName("Sale")
            .AsBitcoinSale(btcAccountId, fiatAccountId, 30_000, 150m));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 20)).WithName("BTC spend")
            .AsBitcoinExpense(btcAccountId, 20_000));

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new StackBitcoinProgressCalculator(_localDatabase);
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.StackBitcoin, entity.GoalTypeJson));
        var calculatedSats = ((StackBitcoinGoalType)progress.UpdatedGoalType).CalculatedSats;

        // Act
        var rows = await Dispatch(goalId);

        // Assert: four rows in date order with natural signs; final net sats reconcile
        Assert.That(rows, Has.Count.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(r => r.Description),
                Is.EqualTo(new[] { "Purchase", "BTC income", "Sale", "BTC spend" }));
            Assert.That(rows.Select(r => r.RunningTotal),
                Is.EqualTo(new[] { 100_000m, 150_000m, 120_000m, 100_000m }));
            Assert.That(rows[3].RunningTotal, Is.EqualTo(calculatedSats));
        });
    }

    [Test]
    public async Task IncomeBtc_NativeSats_Reconciles()
    {
        // Arrange: two direct bitcoin incomes on distinct days
        var goalId = await SeedGoal(
            GoalBuilder.AGoal().WithGoalType(new IncomeBtcGoalType(1_000_000)), new DateOnly(2024, 6, 15));
        var btcAccount = SeedBtcAccount();
        var btcAccountId = new AccountId(btcAccount.Id.ToString());

        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10)).WithName("Mining reward")
            .AsBitcoinIncome(btcAccountId, 100_000));
        SeedTransaction(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 20)).WithName("BTC paycheck")
            .AsBitcoinIncome(btcAccountId, 50_000));

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new IncomeBtcProgressCalculator(_localDatabase);
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.IncomeBtc, entity.GoalTypeJson));
        var calculatedSats = ((IncomeBtcGoalType)progress.UpdatedGoalType).CalculatedSats;

        // Act
        var rows = await Dispatch(goalId);

        // Assert: native sats carried exactly; cumulative running total reconciles
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Description, Is.EqualTo("Mining reward"));
            Assert.That(rows[1].Description, Is.EqualTo("BTC paycheck"));
            Assert.That(rows[0].SatsAmount.Sats, Is.EqualTo(100_000));
            Assert.That(rows[1].SatsAmount.Sats, Is.EqualTo(50_000));
            Assert.That(rows.Select(r => r.RunningTotal), Is.EqualTo(new[] { 100_000m, 150_000m }));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(calculatedSats));
        });
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

    [Test]
    public async Task IncomeFiat_Reconciles_AndExcludesBitcoinToFiat()
    {
        // Arrange: two fiat incomes on distinct days plus a BitcoinToFiat sale in period
        var goalId = await SeedGoal(
            GoalBuilder.AGoal().WithGoalType(new IncomeFiatGoalType(2000m)), new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedIncome(account, 400m, new DateOnly(2024, 6, 10), "Salary A");
        SeedIncome(account, 600m, new DateOnly(2024, 6, 20), "Salary B");
        var sale = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 15))
            .WithName("Bitcoin sale")
            .AsBitcoinSale(satAmount: 100_000, fiatAmount: 100m)
            .Build();
        _localDatabase.GetTransactions().Insert(sale);

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new IncomeFiatProgressCalculator(NewReader());
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.IncomeFiat, entity.GoalTypeJson));
        var calculatedIncome = ((IncomeFiatGoalType)progress.UpdatedGoalType).CalculatedIncome;

        // Act
        var rows = await Dispatch(goalId);

        // Assert: only the fiat income rows appear (BitcoinToFiat is a transfer, not fiat income)
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Description, Is.EqualTo("Salary A"));
            Assert.That(rows[1].Description, Is.EqualTo("Salary B"));
            Assert.That(rows[0].Date, Is.LessThan(rows[1].Date));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(400m));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(1000m));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(calculatedIncome));
        });
    }

    [Test]
    public async Task SaveFiat_MixedSigns_Ordered_Reconciles()
    {
        // Arrange: expense 300 (day 2), income 500 (day 5), expense 100 (day 8)
        var goalId = await SeedGoal(GoalBuilder.ASaveFiatGoal(1000m), new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedExpense(account, 300m, new DateOnly(2024, 6, 2), "Expense early");
        SeedIncome(account, 500m, new DateOnly(2024, 6, 5), "Income");
        SeedExpense(account, 100m, new DateOnly(2024, 6, 8), "Expense late");

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new SaveFiatProgressCalculator(NewReader());
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.SaveFiat, entity.GoalTypeJson));
        var calculatedSavings = ((SaveFiatGoalType)progress.UpdatedGoalType).CalculatedSavings;

        // Act
        var rows = await Dispatch(goalId);

        // Assert: ascending expense→income→expense; running total is cumulative income minus expenses
        Assert.That(rows, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Description, Is.EqualTo("Expense early"));
            Assert.That(rows[1].Description, Is.EqualTo("Income"));
            Assert.That(rows[2].Description, Is.EqualTo("Expense late"));
            Assert.That(rows.Select(r => r.RunningTotal), Is.EqualTo(new[] { -300m, 200m, 100m }));
            Assert.That(rows[2].RunningTotal, Is.EqualTo(calculatedSavings));
        });
    }

    [Test]
    public async Task SaveFiat_AddRemove_ChangesSetPositionAndFinalTotal()
    {
        // Arrange: baseline income 500 (day 10) minus expense 300 (day 5) = 200
        var goalId = await SeedGoal(GoalBuilder.ASaveFiatGoal(1000m), new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedExpense(account, 300m, new DateOnly(2024, 6, 5), "Expense");
        SeedIncome(account, 500m, new DateOnly(2024, 6, 10), "Income");

        var first = await Dispatch(goalId);
        Assert.That(first, Has.Count.EqualTo(2));
        Assert.That(first[1].RunningTotal, Is.EqualTo(200m));

        // Act: add an earlier income — must appear in chronological position and move the total
        var earlyIncome = SeedIncome(account, 200m, new DateOnly(2024, 6, 2), "Early income");

        var afterAdd = await Dispatch(goalId);
        Assert.That(afterAdd, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(afterAdd[0].Description, Is.EqualTo("Early income"));
            Assert.That(afterAdd[0].Date, Is.LessThan(afterAdd[1].Date));
            Assert.That(afterAdd[2].RunningTotal, Is.EqualTo(400m));
        });

        // Act: remove it — set and total revert
        _localDatabase.GetTransactions().Delete(earlyIncome.Id);

        var afterRemove = await Dispatch(goalId);
        Assert.That(afterRemove, Has.Count.EqualTo(2));
        Assert.That(afterRemove[1].RunningTotal, Is.EqualTo(200m));
    }

    [Test]
    public async Task ReduceExpenseCategory_CategoryParity()
    {
        // Arrange: one expense in the goal's category, an equally-sized one outside it
        var targetCategoryId = "65f1a2b3c4d5e6f7890a1b2c";
        var otherCategoryId = "65f1a2b3c4d5e6f7890a1b2d";
        var goalId = await SeedGoal(
            GoalBuilder.AReduceExpenseCategoryGoal(500m, targetCategoryId, "Food"), new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        _localDatabase.GetCategories().Insert(CategoryBuilder.ACategory()
            .WithId(new CategoryId(targetCategoryId)).WithName("Food").Build());
        _localDatabase.GetCategories().Insert(CategoryBuilder.ACategory()
            .WithId(new CategoryId(otherCategoryId)).WithName("Leisure").Build());
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        var inCategory = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10))
            .WithName("Food expense")
            .AsFiatExpense(new AccountId(account.Id.ToString()), 200m)
            .WithCategoryId(new CategoryId(targetCategoryId))
            .Build();
        _localDatabase.GetTransactions().Insert(inCategory);
        var outOfCategory = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 12))
            .WithName("Leisure expense")
            .AsFiatExpense(new AccountId(account.Id.ToString()), 300m)
            .WithCategoryId(new CategoryId(otherCategoryId))
            .Build();
        _localDatabase.GetTransactions().Insert(outOfCategory);

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new ReduceExpenseCategoryProgressCalculator(NewReader());
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.ReduceExpenseCategory, entity.GoalTypeJson));
        var calculatedSpending = ((ReduceExpenseCategoryGoalType)progress.UpdatedGoalType).CalculatedSpending;

        // Act
        var rows = await Dispatch(goalId);

        // Assert: exactly the in-category row; final total equals CalculatedSpending
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Description, Is.EqualTo("Food expense"));
            Assert.That(rows[0].CategoryName, Is.EqualTo("Food"));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(200m));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(calculatedSpending));
        });
    }

    [Test]
    public async Task SavingsRate_PercentageRunningTotal_Reconciles()
    {
        // Arrange: income 1000 (day 3), expense 400 (day 6) → savings rate 60%
        var goalId = await SeedGoal(GoalBuilder.ASavingsRateGoal(20m), new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedIncome(account, 1000m, new DateOnly(2024, 6, 3), "Income");
        SeedExpense(account, 400m, new DateOnly(2024, 6, 6), "Expense");

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new SavingsRateProgressCalculator(NewReader());
        var progress = await calculator.CalculateProgressAsync(
            NewInput(GoalTypeNames.SavingsRate, entity.GoalTypeJson));
        var calculatedPercentage = ((SavingsRateGoalType)progress.UpdatedGoalType).CalculatedPercentage;

        // Act
        var rows = await Dispatch(goalId);

        // Assert: per-row running totals are the incremental percentages
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].RunningTotal, Is.EqualTo(100.00m)); // income only: (1000-0)/1000
            Assert.That(rows[1].RunningTotal, Is.EqualTo(60.00m));  // (1000-400)/1000
            Assert.That(rows[1].RunningTotal, Is.EqualTo(calculatedPercentage));
        });
    }

    [Test]
    public async Task SavingsRate_ExpenseBeforeIncome_FirstRowIsZero()
    {
        // Arrange: expense 400 (day 2) before income 1000 (day 5)
        var goalId = await SeedGoal(GoalBuilder.ASavingsRateGoal(20m), new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 7, 7), 50_000m);

        SeedExpense(account, 400m, new DateOnly(2024, 6, 2), "Expense");
        SeedIncome(account, 1000m, new DateOnly(2024, 6, 5), "Income");

        // Act
        var rows = await Dispatch(goalId);

        // Assert: cumulative income <= 0 branch → first row is 0.00, second reconciles to 60.00
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].RunningTotal, Is.EqualTo(0.00m));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(60.00m));
        });
    }

    [Test]
    public async Task FiatIncome_Sats_AtTransactionDatePrice()
    {
        // Arrange: BTC price seeded per-day — 40k through Jun 15, 60k from Jun 16
        var goalId = await SeedGoal(
            GoalBuilder.AGoal().WithGoalType(new IncomeFiatGoalType(1000m)), new DateOnly(2024, 6, 15));
        var account = SeedUsdAccount();
        SeedPrices(new DateOnly(2024, 5, 24), new DateOnly(2024, 6, 15), 40_000m);
        SeedPrices(new DateOnly(2024, 6, 16), new DateOnly(2024, 7, 7), 60_000m);

        SeedIncome(account, 100m, new DateOnly(2024, 6, 10), "At 40k day");
        SeedIncome(account, 100m, new DateOnly(2024, 6, 20), "At 60k day");

        // Act
        var rows = await Dispatch(goalId);

        // Assert: each fiat income row's sats match its own date's seeded price (GOL-05 precision probe)
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

    private AccountEntity SeedBtcAccount(string name = "Savings BTC")
    {
        var account = BtcAccountBuilder.AnAccount()
            .WithName(name)
            .Build();
        _localDatabase.GetAccounts().Insert(account);
        return account;
    }

    private TransactionEntity SeedTransaction(TransactionBuilder builder)
    {
        var transaction = builder.Build();
        _localDatabase.GetTransactions().Insert(transaction);
        return transaction;
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

    private TransactionEntity SeedIncome(AccountEntity account, decimal amount, DateOnly date, string name = "Income")
    {
        var transaction = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName(name)
            .AsFiatIncome(new AccountId(account.Id.ToString()), amount)
            .Build();
        _localDatabase.GetTransactions().Insert(transaction);
        return transaction;
    }

    private async Task<string> SeedGoal(GoalBuilder builder, DateOnly refDate)
    {
        var goal = builder
            .WithRefDate(refDate)
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);
        return goal.Id.Value;
    }

    private GoalTransactionReader NewReader() =>
        new(_localDatabase, _priceDatabase,
            new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>()));

    private static GoalProgressInput NewInput(GoalTypeNames typeName, string goalTypeJson) =>
        new(typeName, goalTypeJson, new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 30));

    private async Task<IReadOnlyList<ContributingTransactionRow>> Dispatch(string goalId)
    {
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goalId });
        Assert.That(result.IsSuccess, Is.True);
        return ((GoalContributingTransactionsResult.Supported)result.Value!).Rows;
    }
}
