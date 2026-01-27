using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Tests.Builders;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.Services.Goals;

[TestFixture]
public class IncomeBtcProgressCalculatorTests : DatabaseTest
{
    private IncomeBtcProgressCalculator _calculator = null!;

    [SetUp]
    public new void SetUp()
    {
        _calculator = new IncomeBtcProgressCalculator(_localDatabase);

        // Clear transactions before each test
        _localDatabase.GetTransactions().DeleteAll();
    }

    private static string SerializeGoalType(long targetSats, long calculatedSats = 0)
    {
        return JsonSerializer.Serialize(new { targetSats, calculatedSats });
    }

    #region Progress Calculation Tests

    [Test]
    public async Task Should_Calculate_Progress_Based_On_BTC_Income()
    {
        // Arrange: Goal to earn 1,000,000 sats in January 2024
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC income totaling 500,000 sats
        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 200_000);
        AddDirectBtcIncome(new DateOnly(2024, 1, 15), 300_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 500,000 / 1,000,000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(((IncomeBtcGoalType)result.UpdatedGoalType).CalculatedSats, Is.EqualTo(500_000L));
    }

    [Test]
    public async Task Should_Not_Count_Bitcoin_Purchases_As_Income()
    {
        // Arrange: Goal to earn 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC income
        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 300_000);
        // Add BTC purchase (should not count as income)
        AddBtcPurchase(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only direct BTC income counts = 300,000 / 1,000,000 = 30%
        Assert.That(result.Progress, Is.EqualTo(30m));
    }

    [Test]
    public async Task Should_Not_Count_Bitcoin_Expenses()
    {
        // Arrange: Goal to earn 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC income
        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 500_000);
        // Add BTC expense (should not count, even negatively)
        AddDirectBtcExpense(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only income counts (not reduced by expenses) = 500,000 / 1,000,000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Cap_Progress_At_100_Percent()
    {
        // Arrange: Goal to earn 100,000 sats
        var goalTypeJson = SerializeGoalType(100_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add income exceeding target
        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 150_000);

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
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 2, 1),  // February
            new DateOnly(2024, 2, 29));

        // Add income in January (outside period)
        AddDirectBtcIncome(new DateOnly(2024, 1, 15), 500_000);
        // Add income in February (inside period)
        AddDirectBtcIncome(new DateOnly(2024, 2, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only February income counted = 200,000 / 1,000,000 = 20%
        Assert.That(result.Progress, Is.EqualTo(20m));
    }

    [Test]
    public async Task Should_Return_Zero_When_No_Transactions()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
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
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 100_000);

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
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add income on the start date
        AddDirectBtcIncome(new DateOnly(2024, 1, 1), 250_000);

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
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add income on the end date
        AddDirectBtcIncome(new DateOnly(2024, 1, 31), 250_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Not_Count_BitcoinToBitcoin_Transfers()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC income
        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 300_000);
        // Add BitcoinToBitcoin transfer (should not count)
        AddBtcToBtcTransfer(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only the income should count = 300,000 / 1,000,000 = 30%
        Assert.That(result.Progress, Is.EqualTo(30m));
    }

    [Test]
    public async Task Should_Calculate_Total_Bitcoin_Income_Only()
    {
        // Arrange: Goal to earn 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add various transactions - only income should count
        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 300_000);    // +300,000 (income)
        AddDirectBtcIncome(new DateOnly(2024, 1, 8), 200_000);    // +200,000 (income)
        AddBtcPurchase(new DateOnly(2024, 1, 15), 500_000);       // Should not count
        AddDirectBtcExpense(new DateOnly(2024, 1, 20), 100_000);  // Should not count
        AddBtcToBtcTransfer(new DateOnly(2024, 1, 25), 300_000);  // Should not count

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only income counts = 300,000 + 200,000 = 500,000 / 1,000,000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Not_Count_Bitcoin_Sales()
    {
        // Arrange: Goal to earn 1,000,000 sats
        var goalTypeJson = SerializeGoalType(1_000_000);

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeBtc,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC income
        AddDirectBtcIncome(new DateOnly(2024, 1, 5), 400_000);
        // Add BTC sale (should not count as income)
        AddBtcSale(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only direct income counts = 400,000 / 1,000,000 = 40%
        Assert.That(result.Progress, Is.EqualTo(40m));
    }

    #endregion

    #region Helper Methods

    private void AddDirectBtcIncome(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Income")
            .AsBitcoinIncome(satAmount)
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

    private void AddBtcPurchase(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Purchase")
            .AsBitcoinPurchase(satAmount: satAmount, fiatAmount: 50000m)
            .Build();
        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddBtcSale(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Sale")
            .AsBitcoinSale(satAmount: satAmount, fiatAmount: 50000m)
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

    #endregion
}
