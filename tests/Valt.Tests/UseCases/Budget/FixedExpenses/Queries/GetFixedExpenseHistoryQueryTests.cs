using LiteDB;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Tests.Builders;

namespace Valt.Tests.UseCases.Budget.FixedExpenses.Queries;

[TestFixture]
public class GetFixedExpenseHistoryQueryTests : DatabaseTest
{
    private FixedExpenseId _fixedExpenseId = null!;
    private AccountId _fiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fixedExpenseId = IdGenerator.Generate();
        _fiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        // Create account
        var fiatAccount = new FiatAccountBuilder()
        {
            Id = _fiatAccountId,
            Name = "Test Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1000m
        }.Build();
        _localDatabase.GetAccounts().Insert(fiatAccount);

        // Create category
        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithIcon(Icon.Empty)
            .WithName("Test Category")
            .Build();
        _localDatabase.GetCategories().Insert(category);

        // Create fixed expense with price history (multiple ranges)
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(_fiatAccountId)
            .WithId(_fixedExpenseId)
            .WithName("Internet Bill")
            .WithCategoryId(_categoryId)
            .WithFixedAmountRange(100m, FixedExpensePeriods.Monthly, new DateOnly(2023, 1, 1), 15)
            .WithFixedAmountRange(120m, FixedExpensePeriods.Monthly, new DateOnly(2023, 6, 1), 15)
            .WithFixedAmountRange(150m, FixedExpensePeriods.Monthly, new DateOnly(2024, 1, 1), 15)
            .Build();
        _localDatabase.GetFixedExpenses().Insert(fixedExpense);

        // Create transactions bound to the fixed expense
        var transaction1 = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 15),
            Name = "Internet January",
            TransactionDetails = new FiatDetails(_fiatAccountId, 100m, true)
        }.BuildDomainObject();
        _localDatabase.GetTransactions().Insert(transaction1.AsEntity());

        var transaction2 = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 2, 15),
            Name = "Internet February",
            TransactionDetails = new FiatDetails(_fiatAccountId, 100m, true)
        }.BuildDomainObject();
        _localDatabase.GetTransactions().Insert(transaction2.AsEntity());

        var transaction3 = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 7, 15),
            Name = "Internet July",
            TransactionDetails = new FiatDetails(_fiatAccountId, 120m, true)
        }.BuildDomainObject();
        _localDatabase.GetTransactions().Insert(transaction3.AsEntity());

        // Create fixed expense records linking transactions to the fixed expense
        _localDatabase.GetFixedExpenseRecords().Insert(new FixedExpenseRecordEntity
        {
            FixedExpense = fixedExpense,
            Transaction = transaction1.AsEntity(),
            ReferenceDate = new DateTime(2023, 1, 15),
            FixedExpenseRecordStateId = (int)FixedExpenseRecordState.Paid
        });

        _localDatabase.GetFixedExpenseRecords().Insert(new FixedExpenseRecordEntity
        {
            FixedExpense = fixedExpense,
            Transaction = transaction2.AsEntity(),
            ReferenceDate = new DateTime(2023, 2, 15),
            FixedExpenseRecordStateId = (int)FixedExpenseRecordState.Paid
        });

        _localDatabase.GetFixedExpenseRecords().Insert(new FixedExpenseRecordEntity
        {
            FixedExpense = fixedExpense,
            Transaction = transaction3.AsEntity(),
            ReferenceDate = new DateTime(2023, 7, 15),
            FixedExpenseRecordStateId = (int)FixedExpenseRecordState.Paid
        });
    }

    #region GetFixedExpenseHistoryAsync Tests

    [Test]
    public async Task Should_Return_Null_When_FixedExpense_Not_Found()
    {
        // Arrange
        var nonExistentId = new FixedExpenseId(ObjectId.NewObjectId().ToString());
        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Should_Return_FixedExpense_History_With_Transactions()
    {
        // Arrange
        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(_fixedExpenseId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FixedExpenseId, Is.EqualTo(_fixedExpenseId.Value));
        Assert.That(result.FixedExpenseName, Is.EqualTo("Internet Bill"));
        Assert.That(result.Transactions, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Should_Return_Transactions_Sorted_By_Date_Descending()
    {
        // Arrange
        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(_fixedExpenseId);

        // Assert
        Assert.That(result, Is.Not.Null);
        var dates = result!.Transactions.Select(t => t.Date).ToList();
        Assert.That(dates, Is.EqualTo(dates.OrderByDescending(d => d).ToList()));
    }

    [Test]
    public async Task Should_Return_Price_History_With_All_Ranges()
    {
        // Arrange
        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(_fixedExpenseId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PriceHistory, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Should_Return_Price_History_Sorted_By_PeriodStart_Descending()
    {
        // Arrange
        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(_fixedExpenseId);

        // Assert
        Assert.That(result, Is.Not.Null);
        var periodStarts = result!.PriceHistory.Select(p => p.PeriodStart).ToList();
        Assert.That(periodStarts, Is.EqualTo(periodStarts.OrderByDescending(d => d).ToList()));
    }

    [Test]
    public async Task Should_Return_Correct_Transaction_Details()
    {
        // Arrange
        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(_fixedExpenseId);

        // Assert
        Assert.That(result, Is.Not.Null);
        var firstTransaction = result!.Transactions.First();
        Assert.That(firstTransaction.Name, Is.EqualTo("Internet July"));
        Assert.That(firstTransaction.CategoryName, Is.EqualTo("Test Category"));
        Assert.That(firstTransaction.AccountName, Is.EqualTo("Test Account"));
    }

    [Test]
    public async Task Should_Return_Correct_Price_History_Details()
    {
        // Arrange
        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(_fixedExpenseId);

        // Assert
        Assert.That(result, Is.Not.Null);
        var latestRange = result!.PriceHistory.First();
        Assert.That(latestRange.PeriodStart, Is.EqualTo(new DateOnly(2024, 1, 1)));
        Assert.That(latestRange.Period, Is.EqualTo("Monthly"));
        Assert.That(latestRange.Day, Is.EqualTo(15));
    }

    [Test]
    public async Task Should_Return_Empty_Transactions_When_No_Records_Bound()
    {
        // Arrange
        var emptyFixedExpenseId = IdGenerator.Generate();
        var emptyFixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(_fiatAccountId)
            .WithId(emptyFixedExpenseId)
            .WithName("Empty Fixed Expense")
            .WithCategoryId(_categoryId)
            .WithFixedAmountRange(50m, FixedExpensePeriods.Monthly, new DateOnly(2023, 1, 1), 10)
            .Build();
        _localDatabase.GetFixedExpenses().Insert(emptyFixedExpense);

        var query = new FixedExpenseQueries(_localDatabase);

        // Act
        var result = await query.GetFixedExpenseHistoryAsync(emptyFixedExpenseId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Transactions, Is.Empty);
        Assert.That(result.PriceHistory, Has.Count.EqualTo(1));
    }

    #endregion
}
