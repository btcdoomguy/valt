using LiteDB;
using Valt.App.Modules.Budget.Transactions.Commands.BulkEditTransactions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class BulkEditTransactionsHandlerTests : DatabaseTest
{
    private AccountId _accountId = null!;
    private CategoryId _categoryId = null!;
    private CategoryId _newCategoryId = null!;

    protected override async Task SeedDatabase()
    {
        _accountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();
        _newCategoryId = IdGenerator.Generate();

        var account = new FiatAccountBuilder()
        {
            Id = _accountId,
            Name = "Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1000m
        }.Build();
        _localDatabase.GetAccounts().Insert(account);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithIcon(Icon.Empty)
            .WithName("Category")
            .Build();
        _localDatabase.GetCategories().Insert(category);

        var newCategory = new CategoryBuilder()
            .WithId(_newCategoryId)
            .WithIcon(Icon.Empty)
            .WithName("New Category")
            .Build();
        _localDatabase.GetCategories().Insert(newCategory);

        var first = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "First",
            TransactionDetails = new FiatDetails(_accountId, 200, true)
        }.BuildDomainObject();
        _localDatabase.GetTransactions().Insert(first.AsEntity());

        var second = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 2),
            Name = "Second",
            TransactionDetails = new FiatDetails(_accountId, 300, true)
        }.BuildDomainObject();
        _localDatabase.GetTransactions().Insert(second.AsEntity());
    }

    [Test]
    public async Task BulkEdit_With_CategoryOnly_Changes_Category()
    {
        // Arrange
        var handler = new BulkEditTransactionsHandler(_transactionRepository, _categoryRepository);
        var transactions = _localDatabase.GetTransactions().FindAll().ToList();
        var ids = transactions.Select(t => t.Id.ToString()).ToArray();

        // Act
        var result = await handler.HandleAsync(new BulkEditTransactionsCommand
        {
            TransactionIds = ids,
            NewCategoryId = _newCategoryId.Value
        });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UpdatedCount, Is.EqualTo(2));

        foreach (var transaction in _localDatabase.GetTransactions().FindAll())
        {
            Assert.That(transaction.CategoryId.ToString(), Is.EqualTo(_newCategoryId.Value));
        }
    }

    [Test]
    public async Task BulkEdit_With_NameOnly_Changes_Name()
    {
        // Arrange
        var handler = new BulkEditTransactionsHandler(_transactionRepository, _categoryRepository);
        var transactions = _localDatabase.GetTransactions().FindAll().ToList();
        var ids = transactions.Select(t => t.Id.ToString()).ToArray();

        // Act
        var result = await handler.HandleAsync(new BulkEditTransactionsCommand
        {
            TransactionIds = ids,
            NewName = "Renamed"
        });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UpdatedCount, Is.EqualTo(2));

        foreach (var transaction in _localDatabase.GetTransactions().FindAll())
        {
            Assert.That(transaction.Name, Is.EqualTo("Renamed"));
        }
    }

    [Test]
    public async Task BulkEdit_With_Name_And_Category_Changes_Both()
    {
        // Arrange
        var handler = new BulkEditTransactionsHandler(_transactionRepository, _categoryRepository);
        var transactions = _localDatabase.GetTransactions().FindAll().ToList();
        var ids = transactions.Select(t => t.Id.ToString()).ToArray();

        // Act
        var result = await handler.HandleAsync(new BulkEditTransactionsCommand
        {
            TransactionIds = ids,
            NewName = "Renamed",
            NewCategoryId = _newCategoryId.Value
        });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UpdatedCount, Is.EqualTo(2));

        foreach (var transaction in _localDatabase.GetTransactions().FindAll())
        {
            Assert.That(transaction.Name, Is.EqualTo("Renamed"));
            Assert.That(transaction.CategoryId.ToString(), Is.EqualTo(_newCategoryId.Value));
        }
    }

    [Test]
    public async Task BulkEdit_With_Empty_TransactionIds_Fails()
    {
        // Arrange
        var handler = new BulkEditTransactionsHandler(_transactionRepository, _categoryRepository);

        // Act
        var result = await handler.HandleAsync(new BulkEditTransactionsCommand
        {
            TransactionIds = Array.Empty<string>(),
            NewCategoryId = _newCategoryId.Value
        });

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task BulkEdit_With_No_Operation_Fails()
    {
        // Arrange
        var handler = new BulkEditTransactionsHandler(_transactionRepository, _categoryRepository);
        var transaction = _localDatabase.GetTransactions().FindAll().First();

        // Act
        var result = await handler.HandleAsync(new BulkEditTransactionsCommand
        {
            TransactionIds = new[] { transaction.Id.ToString() }
        });

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task BulkEdit_With_Invalid_Category_Fails()
    {
        // Arrange
        var handler = new BulkEditTransactionsHandler(_transactionRepository, _categoryRepository);
        var transaction = _localDatabase.GetTransactions().FindAll().First();

        // Act
        var result = await handler.HandleAsync(new BulkEditTransactionsCommand
        {
            TransactionIds = new[] { transaction.Id.ToString() },
            NewCategoryId = ObjectId.NewObjectId().ToString()
        });

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }
}
