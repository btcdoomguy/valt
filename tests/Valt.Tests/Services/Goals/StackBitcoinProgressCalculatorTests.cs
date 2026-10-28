using LiteDB;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Tests.Builders;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.Services.Goals;

[TestFixture]
public class StackBitcoinProgressCalculatorTests : DatabaseTest
{
    private StackBitcoinProgressCalculator _calculator = null!;

    [SetUp]
    public new void SetUp()
    {
        _calculator = new StackBitcoinProgressCalculator(_localDatabase);

        // Clear transactions before each test
        _localDatabase.GetTransactions().DeleteAll();
    }

    private static string SerializeGoalType(long targetSats, long calculatedSats = 0)
    {
        return JsonSerializer.Serialize(new { targetSats, calculatedSats });
    }

    #region Progress Calculation Tests

    [Test]
    public async Task Should_Calculate_Progress_Based_On_BTC_Transactions()
    {
        // Arrange: Goal to stack 1,000,000 sats in January 2024
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC transactions totaling 500,000 sats
        AddBtcTransaction(new DateOnly(2024, 1, 5), 200_000);
        AddBtcTransaction(new DateOnly(2024, 1, 15), 300_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 500,000 / 1,000,000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(((StackBitcoinGoalType)result.UpdatedGoalType).CalculatedSats, Is.EqualTo(500_000L));
    }

    [Test]
    public async Task Should_Return_CalculatedSats_With_Net_Amount()
    {
        // Arrange: Goal to stack 1,000,000 sats in January 2024
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC purchase
        AddBtcTransaction(new DateOnly(2024, 1, 5), 800_000);
        // Sell some BTC
        AddBtcSale(new DateOnly(2024, 1, 15), 300_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Net = 800,000 - 300,000 = 500,000
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(((StackBitcoinGoalType)result.UpdatedGoalType).CalculatedSats, Is.EqualTo(500_000L));
    }

    [Test]
    public async Task Should_Cap_Progress_At_100_Percent()
    {
        // Arrange: Goal to stack 100,000 sats
        var goalTypeJson = SerializeGoalType(100_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add transactions exceeding target
        AddBtcTransaction(new DateOnly(2024, 1, 5), 150_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Capped at 100%
        Assert.That(result.Progress, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Only_Count_Transactions_In_Period()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 2, 1),  // February
            new DateOnly(2024, 2, 29));

        // Add transaction in January (outside period)
        AddBtcTransaction(new DateOnly(2024, 1, 15), 500_000);
        // Add transaction in February (inside period)
        AddBtcTransaction(new DateOnly(2024, 2, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only February transaction counted = 20%
        Assert.That(result.Progress, Is.EqualTo(20m));
    }

    [Test]
    public async Task Should_Return_Zero_When_No_Transactions()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // No transactions added

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Return_Zero_When_Target_Is_Zero()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(0);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddBtcTransaction(new DateOnly(2024, 1, 5), 100_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_Start_Date()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add transaction on the start date
        AddBtcTransaction(new DateOnly(2024, 1, 1), 250_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_End_Date()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add transaction on the end date
        AddBtcTransaction(new DateOnly(2024, 1, 31), 250_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Calculate_Net_Stacked_When_Selling_BTC()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add positive BTC transaction (purchase)
        AddBtcTransaction(new DateOnly(2024, 1, 5), 500_000);
        // Sell some BTC (converts to fiat)
        AddBtcSpendTransaction(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Net = 500,000 - 200,000 = 300,000 / 1,000,000 = 30%
        Assert.That(result.Progress, Is.EqualTo(30m));
    }

    [Test]
    public async Task Should_Not_Count_BitcoinToBitcoin_Transfers()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC purchase
        AddBtcTransaction(new DateOnly(2024, 1, 5), 300_000);
        // Add BitcoinToBitcoin transfer (moving BTC between accounts, not stacking)
        AddBtcToBtcTransfer(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only the purchase should count (300,000 / 1,000,000 = 30%)
        Assert.That(result.Progress, Is.EqualTo(30m));
    }

    [Test]
    public async Task Should_Count_Direct_Bitcoin_Income()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC purchase
        AddBtcTransaction(new DateOnly(2024, 1, 5), 300_000);
        // Add direct BTC income (earning BTC directly)
        AddDirectBtcIncome(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Both should count (300,000 + 200,000 = 500,000 / 1,000,000 = 50%)
        Assert.That(result.Progress, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Subtract_Bitcoin_Sales_From_Progress()
    {
        // Arrange: Goal to stack 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC purchase of 900,000 sats
        AddBtcTransaction(new DateOnly(2024, 1, 5), 900_000);
        // Sell 400,000 sats (convert to fiat)
        AddBtcSale(new DateOnly(2024, 1, 15), 400_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Net = 900,000 - 400,000 = 500,000 / 1,000,000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Subtract_Bitcoin_Expenses_From_Progress()
    {
        // Arrange: Goal to stack 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC purchase of 800,000 sats
        AddBtcTransaction(new DateOnly(2024, 1, 5), 800_000);
        // Spend 300,000 sats directly (direct BTC expense)
        AddDirectBtcExpense(new DateOnly(2024, 1, 15), 300_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Net = 800,000 - 300,000 = 500,000 / 1,000,000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Return_Zero_When_Sales_Exceed_Purchases()
    {
        // Arrange: Goal to stack 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC purchase of 300,000 sats
        AddBtcTransaction(new DateOnly(2024, 1, 5), 300_000);
        // Sell 500,000 sats (more than purchased in period)
        AddBtcSale(new DateOnly(2024, 1, 15), 500_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Net = 300,000 - 500,000 = -200,000, but capped at 0%
        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Calculate_Net_With_All_Transaction_Types()
    {
        // Arrange: Goal to stack 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add various transactions
        AddBtcTransaction(new DateOnly(2024, 1, 5), 500_000);    // +500,000 (purchase)
        AddDirectBtcIncome(new DateOnly(2024, 1, 8), 200_000);   // +200,000 (income)
        AddBtcSale(new DateOnly(2024, 1, 15), 100_000);          // -100,000 (sale)
        AddDirectBtcExpense(new DateOnly(2024, 1, 20), 50_000);  // -50,000 (expense)
        AddBtcToBtcTransfer(new DateOnly(2024, 1, 25), 300_000); // 0 (transfer, shouldn't count)

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Net = (500,000 + 200,000) - (100,000 + 50,000) = 550,000 / 1,000,000 = 55%
        Assert.That(result.Progress, Is.EqualTo(55m));
    }

    #endregion

    #region Exclusion Flag Tests

    [Test]
    public async Task Should_Ignore_Excluded_Transactions_In_Progress_And_Rows()
    {
        // Arrange: flagged purchase and flagged sale must not move progress or appear as rows
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddBtcTransaction(new DateOnly(2024, 1, 5), 300_000, excluded: true);  // ignored purchase
        AddBtcTransaction(new DateOnly(2024, 1, 15), 200_000);                 // counts
        AddBtcSale(new DateOnly(2024, 1, 20), 100_000, excluded: true);        // ignored sale

        // Act
        var result = await _calculator.CalculateProgressAsync(input);
        var rows = await _calculator.GetContributingTransactionsAsync(input);

        // Assert: net = 200,000; exactly one row; final running total reconciles with progress
        var calculatedSats = ((StackBitcoinGoalType)result.UpdatedGoalType).CalculatedSats;
        Assert.That(calculatedSats, Is.EqualTo(200_000L));
        Assert.That(result.Progress, Is.EqualTo(20m));
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rows![0].Transaction.Name, Is.EqualTo("BTC Purchase"));
            Assert.That(rows[0].Contribution, Is.EqualTo(200_000m));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(200_000m));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(calculatedSats));
        });
    }

    [Test]
    public async Task Should_Reconcile_Legacy_Modern_And_Excluded_Mix()
    {
        // Arrange: legacy purchase, modern purchase, modern unflagged sale, modern excluded sale
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        InsertLegacyPurchase(new DateOnly(2024, 1, 5), 100_000);   // counts (no excl field)
        AddBtcTransaction(new DateOnly(2024, 1, 10), 50_000);      // counts
        AddBtcSale(new DateOnly(2024, 1, 15), 20_000);             // counts (subtracted)
        AddBtcSale(new DateOnly(2024, 1, 20), 30_000, excluded: true); // ignored

        // Act
        var result = await _calculator.CalculateProgressAsync(input);
        var rows = await _calculator.GetContributingTransactionsAsync(input);

        // Assert: net = 100k + 50k - 20k = 130k; three rows; final running total reconciles
        var calculatedSats = ((StackBitcoinGoalType)result.UpdatedGoalType).CalculatedSats;
        Assert.That(calculatedSats, Is.EqualTo(130_000L));
        Assert.That(result.Progress, Is.EqualTo(13m));
        Assert.That(rows, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(rows!.Select(r => r.Transaction.Name),
                Is.EqualTo(new[] { "Legacy Purchase", "BTC Purchase", "BTC Sale" }));
            Assert.That(rows.Select(r => r.RunningTotal),
                Is.EqualTo(new[] { 100_000m, 150_000m, 130_000m }));
            Assert.That(rows[2].RunningTotal, Is.EqualTo(calculatedSats));
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Inserts a purchase document with the "excl" field stripped, simulating a document
    /// written before the exclusion flag existed (pre-phase-53 legacy shape).
    /// </summary>
    private void InsertLegacyPurchase(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("Legacy Purchase")
            .AsBitcoinPurchase(satAmount: satAmount, fiatAmount: 100m)
            .Build();

        var database = GetUnderlyingDatabase();
        var document = database.Mapper.ToDocument(entity);
        document.Remove("excl");
        database.GetCollection("budget_transactions").Insert(document);
    }

    private LiteDatabase GetUnderlyingDatabase()
    {
        var field = typeof(Valt.Infra.DataAccess.LocalDatabase).GetField("_database",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (LiteDatabase)field!.GetValue(_localDatabase)!;
    }

    private void AddBtcTransaction(DateOnly date, long satAmount, bool excluded = false)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Purchase")
            .AsBitcoinPurchase(satAmount: satAmount, fiatAmount: 100m)
            .WithExcluded(excluded)
            .Build();
        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddBtcSpendTransaction(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Spend")
            .AsBitcoinSale(satAmount: satAmount, fiatAmount: 100m)
            .Build();
        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddBtcToBtcTransfer(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Transfer")
            .AsBitcoinToBitcoinTransfer(satAmount)
            .Build();
        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddDirectBtcIncome(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Income")
            .AsBitcoinIncome(satAmount)
            .Build();
        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddBtcSale(DateOnly date, long satAmount, bool excluded = false)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Sale")
            .AsBitcoinSale(satAmount: satAmount, fiatAmount: 50000m)
            .WithExcluded(excluded)
            .Build();
        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddDirectBtcExpense(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Expense")
            .AsBitcoinExpense(satAmount)
            .Build();
        _localDatabase.GetTransactions().Insert(entity);
    }

    #endregion
}
