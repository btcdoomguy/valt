using NSubstitute;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.UseCases.Goals;

[TestFixture]
public class SpendingLimitProgressCalculatorTests
{
    private SpendingLimitProgressCalculator _calculator = null!;
    private IGoalTransactionReader _transactionReader = null!;

    [SetUp]
    public void SetUp()
    {
        _transactionReader = Substitute.For<IGoalTransactionReader>();
        _calculator = new SpendingLimitProgressCalculator(_transactionReader);
    }

    private static string SerializeGoalType(decimal targetAmount, decimal calculatedSpending = 0)
    {
        return JsonSerializer.Serialize(new { targetAmount, calculatedSpending });
    }

    #region Progress Calculation Tests

    [Test]
    public async Task Should_Calculate_Progress_Based_On_Expenses()
    {
        // Arrange: Goal to limit spending to $1000 in January 2024
        var goalTypeJson = SerializeGoalType(1000m);
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        // Mock transaction reader to return $500 in expenses
        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(500m);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Progress = 500/1000 * 100 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(((SpendingLimitGoalType)result.UpdatedGoalType).CalculatedSpending, Is.EqualTo(500m));
    }

    [Test]
    public async Task Should_Return_0_Progress_When_No_Spending()
    {
        // Arrange: Goal to limit spending to $1000
        var goalTypeJson = SerializeGoalType(1000m);
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        // Mock transaction reader to return $0 in expenses
        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(0m);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Progress is 0% (nothing spent = good)
        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Return_100_Progress_When_At_Limit()
    {
        // Arrange: Goal to limit spending to $1000
        var goalTypeJson = SerializeGoalType(1000m);
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        // Mock transaction reader to return $1000 in expenses (at limit)
        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(1000m);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Progress is 100% (at limit = failed)
        Assert.That(result.Progress, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Return_100_Progress_When_Over_Limit()
    {
        // Arrange: Goal to limit spending to $100
        var goalTypeJson = SerializeGoalType(100m);
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        // Mock transaction reader to return $150 in expenses (over limit)
        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(150m);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Progress is capped at 100% (over limit = failed)
        Assert.That(result.Progress, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Return_100_Progress_When_Target_Is_Zero_With_Spending()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(0m);
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(100m);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 100% because there's spending but no limit (instant fail)
        Assert.That(result.Progress, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Return_0_Progress_When_Target_Is_Zero_With_No_Spending()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(0m);
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(0m);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 0% because no spending
        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Round_Calculated_Spending_To_Two_Decimals()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m);
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        // Return a value with more decimals
        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(123.456789m);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Rounded to 2 decimals
        Assert.That(((SpendingLimitGoalType)result.UpdatedGoalType).CalculatedSpending, Is.EqualTo(123.46m));
    }

    [Test]
    public async Task Should_Pass_Correct_Dates_To_TransactionReader()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m);
        var from = new DateOnly(2024, 2, 1);
        var to = new DateOnly(2024, 2, 29);

        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            goalTypeJson,
            from,
            to);

        _transactionReader.CalculateTotalExpenses(from, to, null).Returns(0m);

        // Act
        await _calculator.CalculateProgressAsync(input);

        // Assert: Verify correct dates were passed
        _transactionReader.Received(1).CalculateTotalExpenses(from, to, null);
    }

    #endregion
}
