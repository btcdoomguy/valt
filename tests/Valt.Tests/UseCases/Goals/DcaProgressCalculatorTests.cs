using LiteDB;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.UseCases.Goals;

[TestFixture]
public class DcaProgressCalculatorTests : DatabaseTest
{
    private DcaProgressCalculator _calculator = null!;

    [SetUp]
    public new void SetUp()
    {
        _calculator = new DcaProgressCalculator(_localDatabase);

        // Clear transactions before each test
        _localDatabase.GetTransactions().DeleteAll();
    }

    private static string SerializeGoalType(int targetPurchaseCount, int calculatedPurchaseCount = 0)
    {
        return JsonSerializer.Serialize(new { targetPurchaseCount, calculatedPurchaseCount });
    }

    #region Progress Calculation Tests

    [Test]
    public async Task Should_Calculate_Progress_Based_On_Purchase_Count()
    {
        // Arrange: Goal to make 4 purchases in January 2024
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add 2 bitcoin purchases
        AddBitcoinPurchase(new DateOnly(2024, 1, 5));
        AddBitcoinPurchase(new DateOnly(2024, 1, 15));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 2 / 4 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(((DcaGoalType)result.UpdatedGoalType).CalculatedPurchaseCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Should_Cap_Progress_At_100_Percent()
    {
        // Arrange: Goal to make 2 purchases
        var goalTypeJson = SerializeGoalType(2);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add 5 purchases (more than target)
        AddBitcoinPurchase(new DateOnly(2024, 1, 1));
        AddBitcoinPurchase(new DateOnly(2024, 1, 8));
        AddBitcoinPurchase(new DateOnly(2024, 1, 15));
        AddBitcoinPurchase(new DateOnly(2024, 1, 22));
        AddBitcoinPurchase(new DateOnly(2024, 1, 29));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Capped at 100%
        Assert.That(result.Progress, Is.EqualTo(100m));
        Assert.That(((DcaGoalType)result.UpdatedGoalType).CalculatedPurchaseCount, Is.EqualTo(5));
    }

    [Test]
    public async Task Should_Only_Count_Transactions_In_Period()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 2, 1),  // February
            new DateOnly(2024, 2, 29));

        // Add purchase in January (outside period)
        AddBitcoinPurchase(new DateOnly(2024, 1, 15));
        // Add purchase in February (inside period)
        AddBitcoinPurchase(new DateOnly(2024, 2, 10));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only February purchase counted = 1 / 4 = 25%
        Assert.That(result.Progress, Is.EqualTo(25m));
        Assert.That(((DcaGoalType)result.UpdatedGoalType).CalculatedPurchaseCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_Return_Zero_When_No_Transactions()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // No transactions added

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(0m));
        Assert.That(((DcaGoalType)result.UpdatedGoalType).CalculatedPurchaseCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Should_Return_Zero_When_Target_Is_Zero()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(0);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddBitcoinPurchase(new DateOnly(2024, 1, 5));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_Start_Date()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add purchase on the start date
        AddBitcoinPurchase(new DateOnly(2024, 1, 1));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 1 / 4 = 25%
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_End_Date()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add purchase on the end date
        AddBitcoinPurchase(new DateOnly(2024, 1, 31));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 1 / 4 = 25%
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Not_Count_Bitcoin_Sales()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add bitcoin purchase
        AddBitcoinPurchase(new DateOnly(2024, 1, 5));
        // Add bitcoin sale (should not count)
        AddBitcoinSale(new DateOnly(2024, 1, 10));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only the purchase counts = 1 / 4 = 25%
        Assert.That(result.Progress, Is.EqualTo(25m));
        Assert.That(((DcaGoalType)result.UpdatedGoalType).CalculatedPurchaseCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_Not_Count_Fiat_Transactions()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add bitcoin purchase
        AddBitcoinPurchase(new DateOnly(2024, 1, 5));
        // Add fiat transaction (should not count)
        AddFiatTransaction(new DateOnly(2024, 1, 10));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only the bitcoin purchase counts = 1 / 4 = 25%
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Not_Count_BitcoinToBitcoin_Transfers()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add bitcoin purchase
        AddBitcoinPurchase(new DateOnly(2024, 1, 5));
        // Add bitcoin transfer (should not count)
        AddBitcoinTransfer(new DateOnly(2024, 1, 10));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only the purchase counts = 1 / 4 = 25%
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Count_Multiple_Purchases_On_Same_Day()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add 2 purchases on the same day
        AddBitcoinPurchase(new DateOnly(2024, 1, 15));
        AddBitcoinPurchase(new DateOnly(2024, 1, 15));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Both purchases count = 2 / 4 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(((DcaGoalType)result.UpdatedGoalType).CalculatedPurchaseCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Should_Calculate_Exact_100_Percent_When_Target_Met()
    {
        // Arrange: Goal to make 4 purchases
        var goalTypeJson = SerializeGoalType(4);

        var input = new GoalProgressInput(
            GoalTypeNames.Dca,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add exactly 4 purchases
        AddBitcoinPurchase(new DateOnly(2024, 1, 7));
        AddBitcoinPurchase(new DateOnly(2024, 1, 14));
        AddBitcoinPurchase(new DateOnly(2024, 1, 21));
        AddBitcoinPurchase(new DateOnly(2024, 1, 28));

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Exactly 100%
        Assert.That(result.Progress, Is.EqualTo(100m));
        Assert.That(((DcaGoalType)result.UpdatedGoalType).CalculatedPurchaseCount, Is.EqualTo(4));
    }

    #endregion

    #region Helper Methods

    private void AddBitcoinPurchase(DateOnly date)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "Bitcoin Purchase",
            Type = TransactionEntityType.FiatToBitcoin,
            FromFiatAmount = -100m, // Fiat spent
            ToSatAmount = 100000,   // Sats received
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = ObjectId.NewObjectId(),
            ToAccountId = ObjectId.NewObjectId(),
            Version = 1
        });
    }

    private void AddBitcoinSale(DateOnly date)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "Bitcoin Sale",
            Type = TransactionEntityType.BitcoinToFiat,
            FromSatAmount = -100000, // Sats sold
            ToFiatAmount = 100m,     // Fiat received
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = ObjectId.NewObjectId(),
            ToAccountId = ObjectId.NewObjectId(),
            Version = 1
        });
    }

    private void AddFiatTransaction(DateOnly date)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "Fiat Transaction",
            Type = TransactionEntityType.Fiat,
            FromFiatAmount = -50m, // Fiat expense
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = ObjectId.NewObjectId(),
            Version = 1
        });
    }

    private void AddBitcoinTransfer(DateOnly date)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "Bitcoin Transfer",
            Type = TransactionEntityType.BitcoinToBitcoin,
            FromSatAmount = -50000, // Sats from source
            ToSatAmount = 50000,    // Sats to destination
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = ObjectId.NewObjectId(),
            ToAccountId = ObjectId.NewObjectId(),
            Version = 1
        });
    }

    #endregion
}
