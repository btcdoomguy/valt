using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Tests.Builders;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.Services.Goals;

[TestFixture]
public class BitcoinHodlProgressCalculatorTests : DatabaseTest
{
    private BitcoinHodlProgressCalculator _calculator = null!;

    [SetUp]
    public new void SetUp()
    {
        _calculator = new BitcoinHodlProgressCalculator(_localDatabase);

        // Clear transactions before each test
        _localDatabase.GetTransactions().DeleteAll();
    }

    private static string SerializeGoalType(long maxSellableSats, long calculatedSoldSats = 0)
    {
        return JsonSerializer.Serialize(new { maxSellableSats, calculatedSoldSats });
    }

    #region Progress Calculation Tests

    [Test]
    public async Task Should_Calculate_Progress_Based_On_Sold_Sats()
    {
        // Arrange: Goal allowing 100,000 sats sold in January 2024
        var goalTypeJson = SerializeGoalType(100_000);

        var input = new GoalProgressInput(
            GoalTypeNames.BitcoinHodl,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Sell 40,000 sats
        AddBtcSale(new DateOnly(2024, 1, 10), 40_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 40,000 / 100,000 = 40%
        Assert.That(result.Progress, Is.EqualTo(40m));
        Assert.That(((BitcoinHodlGoalType)result.UpdatedGoalType).CalculatedSoldSats, Is.EqualTo(40_000L));
    }

    [Test]
    public async Task Should_Return_0_When_Nothing_Sold()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(100_000);

        var input = new GoalProgressInput(
            GoalTypeNames.BitcoinHodl,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Only a purchase — never a sale
        AddBtcPurchase(new DateOnly(2024, 1, 10), 100_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(0m));
        Assert.That(((BitcoinHodlGoalType)result.UpdatedGoalType).CalculatedSoldSats, Is.EqualTo(0L));
    }

    [Test]
    public async Task Should_Cap_Progress_At_100_When_Over_Limit()
    {
        // Arrange: Goal allowing 50,000 sats sold
        var goalTypeJson = SerializeGoalType(50_000);

        var input = new GoalProgressInput(
            GoalTypeNames.BitcoinHodl,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Sell 80,000 sats (over the limit)
        AddBtcSale(new DateOnly(2024, 1, 10), 80_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Capped at 100%
        Assert.That(result.Progress, Is.EqualTo(100m));
        Assert.That(((BitcoinHodlGoalType)result.UpdatedGoalType).CalculatedSoldSats, Is.EqualTo(80_000L));
    }

    [Test]
    public async Task Should_Return_100_When_Any_Sale_In_Full_Hodl_Mode()
    {
        // Arrange: MaxSellableSats == 0 — any sale is an instant fail
        var goalTypeJson = SerializeGoalType(0);

        var input = new GoalProgressInput(
            GoalTypeNames.BitcoinHodl,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddBtcSale(new DateOnly(2024, 1, 10), 10_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Only_Count_Sales_In_Period()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(100_000);

        var input = new GoalProgressInput(
            GoalTypeNames.BitcoinHodl,
            goalTypeJson,
            new DateOnly(2024, 2, 1),  // February
            new DateOnly(2024, 2, 29));

        // Sale in January (outside period)
        AddBtcSale(new DateOnly(2024, 1, 15), 60_000);
        // Sale in February (inside period)
        AddBtcSale(new DateOnly(2024, 2, 10), 40_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only February sale counted = 40%
        Assert.That(result.Progress, Is.EqualTo(40m));
        Assert.That(((BitcoinHodlGoalType)result.UpdatedGoalType).CalculatedSoldSats, Is.EqualTo(40_000L));
    }

    [Test]
    public async Task Should_Not_Count_Direct_Bitcoin_Expenses_As_Sales()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(100_000);

        var input = new GoalProgressInput(
            GoalTypeNames.BitcoinHodl,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddBtcSale(new DateOnly(2024, 1, 10), 40_000);
        // Direct BTC expense (Bitcoin type, negative FromSatAmount) is not a BitcoinToFiat sale
        AddDirectBtcExpense(new DateOnly(2024, 1, 15), 30_000);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only the sale counts
        Assert.That(result.Progress, Is.EqualTo(40m));
        Assert.That(((BitcoinHodlGoalType)result.UpdatedGoalType).CalculatedSoldSats, Is.EqualTo(40_000L));
    }

    #endregion

    #region Exclusion Flag Tests

    [Test]
    public async Task Should_Ignore_Excluded_Sales_In_Progress_And_Rows()
    {
        // Arrange: one unflagged and one excluded sale in the period
        var goalTypeJson = SerializeGoalType(100_000);

        var input = new GoalProgressInput(
            GoalTypeNames.BitcoinHodl,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddBtcSale(new DateOnly(2024, 1, 5), 30_000, excluded: true);  // ignored
        AddBtcSale(new DateOnly(2024, 1, 10), 40_000);                 // counts

        // Act
        var result = await _calculator.CalculateProgressAsync(input);
        var rows = await _calculator.GetContributingTransactionsAsync(input);

        // Assert: sold = 40,000; exactly one row; final running total reconciles with progress
        var calculatedSoldSats = ((BitcoinHodlGoalType)result.UpdatedGoalType).CalculatedSoldSats;
        Assert.That(calculatedSoldSats, Is.EqualTo(40_000L));
        Assert.That(result.Progress, Is.EqualTo(40m));
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rows![0].Transaction.Name, Is.EqualTo("BTC Sale"));
            Assert.That(rows[0].Contribution, Is.EqualTo(40_000m));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(40_000m));
            Assert.That(rows[0].RunningTotal, Is.EqualTo(calculatedSoldSats));
        });
    }

    #endregion

    #region Helper Methods

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

    private void AddBtcPurchase(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("BTC Purchase")
            .AsBitcoinPurchase(satAmount: satAmount, fiatAmount: 100m)
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
