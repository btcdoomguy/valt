using LiteDB;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.UseCases.Goals;

[TestFixture]
public class IncomeFiatProgressCalculatorTests : DatabaseTest
{
    private IncomeFiatProgressCalculator _calculator = null!;
    private ObjectId _usdAccountId;
    private ObjectId _brlAccountId;
    private ObjectId _btcAccountId;

    [SetUp]
    public new void SetUp()
    {
        _calculator = new IncomeFiatProgressCalculator(_localDatabase);

        // Clear data before each test
        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetAccounts().DeleteAll();

        // Create test accounts
        _usdAccountId = CreateFiatAccount("USD");
        _brlAccountId = CreateFiatAccount("BRL");
        _btcAccountId = CreateBtcAccount();
    }

    private static string SerializeGoalType(decimal targetAmount, string currency, decimal calculatedIncome = 0)
    {
        return JsonSerializer.Serialize(new { targetAmount, currency, calculatedIncome });
    }

    #region Progress Calculation Tests

    [Test]
    public async Task Should_Calculate_Progress_Based_On_Fiat_Income()
    {
        // Arrange: Goal to earn $1000 USD in January 2024
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add USD income totaling $500
        AddFiatIncome(new DateOnly(2024, 1, 5), 200m, _usdAccountId);
        AddFiatIncome(new DateOnly(2024, 1, 15), 300m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 500 / 1000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(((IncomeFiatGoalType)result.UpdatedGoalType).CalculatedIncome, Is.EqualTo(500m));
    }

    [Test]
    public async Task Should_Include_Bitcoin_Sales_As_Income()
    {
        // Arrange: Goal to earn $1000 USD
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add fiat income
        AddFiatIncome(new DateOnly(2024, 1, 5), 300m, _usdAccountId);
        // Add bitcoin sale (fiat received from selling bitcoin)
        AddBitcoinSale(new DateOnly(2024, 1, 10), 200m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 300 + 200 = 500 / 1000 = 50%
        Assert.That(result.Progress, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Only_Count_Transactions_In_Specified_Currency()
    {
        // Arrange: Goal to earn $1000 USD
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add USD income
        AddFiatIncome(new DateOnly(2024, 1, 5), 300m, _usdAccountId);
        // Add BRL income (should not count)
        AddFiatIncome(new DateOnly(2024, 1, 10), 500m, _brlAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only USD income counts = 300 / 1000 = 30%
        Assert.That(result.Progress, Is.EqualTo(30m));
    }

    [Test]
    public async Task Should_Cap_Progress_At_100_Percent()
    {
        // Arrange: Goal to earn $100
        var goalTypeJson = SerializeGoalType(100m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add income exceeding target
        AddFiatIncome(new DateOnly(2024, 1, 5), 150m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Capped at 100%
        Assert.That(result.Progress, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Only_Count_Transactions_In_Period()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 2, 1),  // February
            new DateOnly(2024, 2, 29));

        // Add income in January (outside period)
        AddFiatIncome(new DateOnly(2024, 1, 15), 500m, _usdAccountId);
        // Add income in February (inside period)
        AddFiatIncome(new DateOnly(2024, 2, 10), 200m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only February income counted = 200 / 1000 = 20%
        Assert.That(result.Progress, Is.EqualTo(20m));
    }

    [Test]
    public async Task Should_Return_Zero_When_No_Transactions()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
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
        var goalTypeJson = SerializeGoalType(0m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        AddFiatIncome(new DateOnly(2024, 1, 5), 100m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_Start_Date()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add income on the start date
        AddFiatIncome(new DateOnly(2024, 1, 1), 250m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Include_Transaction_On_End_Date()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add income on the end date
        AddFiatIncome(new DateOnly(2024, 1, 31), 250m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Not_Count_Fiat_Expenses()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add fiat income
        AddFiatIncome(new DateOnly(2024, 1, 5), 300m, _usdAccountId);
        // Add fiat expense (negative amount, should not count as income)
        AddFiatExpense(new DateOnly(2024, 1, 10), 500m, _usdAccountId);

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: Only income counts = 300 / 1000 = 30%
        Assert.That(result.Progress, Is.EqualTo(30m));
    }

    [Test]
    public async Task Should_Calculate_Combined_Income_And_Bitcoin_Sales()
    {
        // Arrange
        var goalTypeJson = SerializeGoalType(1000m, "USD");

        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        // Add various income transactions
        AddFiatIncome(new DateOnly(2024, 1, 5), 200m, _usdAccountId);
        AddFiatIncome(new DateOnly(2024, 1, 8), 100m, _usdAccountId);
        AddBitcoinSale(new DateOnly(2024, 1, 15), 250m, _usdAccountId);
        AddFiatExpense(new DateOnly(2024, 1, 20), 1000m, _usdAccountId); // Should not count

        // Act
        var result = await _calculator.CalculateProgressAsync(input);

        // Assert: 200 + 100 + 250 = 550 / 1000 = 55%
        Assert.That(result.Progress, Is.EqualTo(55m));
    }

    #endregion

    #region Helper Methods

    private ObjectId CreateFiatAccount(string currency)
    {
        var accountId = ObjectId.NewObjectId();
        _localDatabase.GetAccounts().Insert(new AccountEntity
        {
            Id = accountId,
            Name = $"Test {currency} Account",
            AccountEntityType = AccountEntityType.Fiat,
            Currency = currency,
            Visible = true,
            Version = 1
        });
        return accountId;
    }

    private ObjectId CreateBtcAccount()
    {
        var accountId = ObjectId.NewObjectId();
        _localDatabase.GetAccounts().Insert(new AccountEntity
        {
            Id = accountId,
            Name = "Test BTC Account",
            AccountEntityType = AccountEntityType.Bitcoin,
            Visible = true,
            Version = 1
        });
        return accountId;
    }

    private void AddFiatIncome(DateOnly date, decimal amount, ObjectId accountId)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "Fiat Income",
            Type = TransactionEntityType.Fiat,
            FromFiatAmount = amount, // Positive for income
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = accountId,
            Version = 1
        });
    }

    private void AddFiatExpense(DateOnly date, decimal amount, ObjectId accountId)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "Fiat Expense",
            Type = TransactionEntityType.Fiat,
            FromFiatAmount = -amount, // Negative for expense
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = accountId,
            Version = 1
        });
    }

    private void AddBitcoinSale(DateOnly date, decimal fiatAmount, ObjectId toAccountId)
    {
        _localDatabase.GetTransactions().Insert(new TransactionEntity
        {
            Id = ObjectId.NewObjectId(),
            Date = date.ToValtDateTime(),
            Name = "Bitcoin Sale",
            Type = TransactionEntityType.BitcoinToFiat,
            FromSatAmount = -100000, // Arbitrary sat amount sold
            ToFiatAmount = fiatAmount, // Positive for fiat received
            CategoryId = ObjectId.NewObjectId(),
            FromAccountId = _btcAccountId,
            ToAccountId = toAccountId,
            Version = 1
        });
    }

    #endregion
}
