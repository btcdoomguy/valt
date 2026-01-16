using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.UseCases.Goals;

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

    #region Progress Calculation Tests

    [Test]
    public async Task Should_Calculate_Progress_Based_On_BTC_Transactions()
    {
        // Arrange: Goal to stack 1,000,000 sats in January 2024
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add BTC transactions totaling 500,000 sats
        AddBtcTransaction(new DateOnly(2024, 1, 5), 200_000);
        AddBtcTransaction(new DateOnly(2024, 1, 15), 300_000);

        // Act
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert: 500,000 / 1,000,000 = 50%
        Assert.That(progress, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Cap_Progress_At_100_Percent()
    {
        // Arrange: Goal to stack 100,000 sats
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(100_000)));

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add transactions exceeding target
        AddBtcTransaction(new DateOnly(2024, 1, 5), 150_000);

        // Act
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert: Capped at 100%
        Assert.That(progress, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Only_Count_Transactions_In_Period()
    {
        // Arrange
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

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
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert: Only February transaction counted = 20%
        Assert.That(progress, Is.EqualTo(20m));
    }

    [Test]
    public async Task Should_Return_Zero_When_No_Transactions()
    {
        // Arrange
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // No transactions added

        // Act
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Return_Zero_When_Target_Is_Zero()
    {
        // Arrange
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(0)));

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddBtcTransaction(new DateOnly(2024, 1, 5), 100_000);

        // Act
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_Start_Date()
    {
        // Arrange
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add transaction on the start date
        AddBtcTransaction(new DateOnly(2024, 1, 1), 250_000);

        // Act
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_End_Date()
    {
        // Arrange
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add transaction on the end date
        AddBtcTransaction(new DateOnly(2024, 1, 31), 250_000);

        // Act
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Only_Count_Positive_ToSatAmount()
    {
        // Arrange
        var goalTypeJson = JsonSerializer.Serialize(
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        var input = new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add positive BTC transaction
        AddBtcTransaction(new DateOnly(2024, 1, 5), 500_000);
        // Add transaction with FromSatAmount (spending, not receiving)
        AddBtcSpendTransaction(new DateOnly(2024, 1, 10), 200_000);

        // Act
        var progress = await _calculator.CalculateProgressAsync(input);

        // Assert: Only the incoming 500,000 sats should count
        Assert.That(progress, Is.EqualTo(50m));
    }

    #endregion

    #region Helper Methods

    private void AddBtcTransaction(DateOnly date, long satAmount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "BTC Purchase",
            ToSatAmount = satAmount,
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = ObjectId.NewObjectId(),
            Version = 1
        });
    }

    private void AddBtcSpendTransaction(DateOnly date, long satAmount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "BTC Spend",
            FromSatAmount = -satAmount,  // Negative to indicate spending
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = ObjectId.NewObjectId(),
            Version = 1
        });
    }

    #endregion
}
