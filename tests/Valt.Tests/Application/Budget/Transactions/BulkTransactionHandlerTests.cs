using Valt.App.Modules.Budget.Transactions.Commands.BulkChangeCategoryTransactions;
using Valt.App.Modules.Budget.Transactions.Commands.BulkRenameTransactions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class BulkTransactionHandlerTests : DatabaseTest
{
    private AccountId _hiddenAccountId = null!;
    private AccountId _visibleAccountId = null!;
    private CategoryId _categoryId = null!;
    private CategoryId _newCategoryId = null!;

    protected override async Task SeedDatabase()
    {
        _hiddenAccountId = IdGenerator.Generate();
        _visibleAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();
        _newCategoryId = IdGenerator.Generate();

        var hiddenAccount = new FiatAccountBuilder()
        {
            Id = _hiddenAccountId,
            Name = "Hidden Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1000m,
            Visible = false
        }.Build();
        _localDatabase.GetAccounts().Insert(hiddenAccount);

        var visibleAccount = new FiatAccountBuilder()
        {
            Id = _visibleAccountId,
            Name = "Visible Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1000m
        }.Build();
        _localDatabase.GetAccounts().Insert(visibleAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithIcon(Icon.Empty)
            .WithName("Test Category")
            .Build();
        _localDatabase.GetCategories().Insert(category);

        var newCategory = new CategoryBuilder()
            .WithId(_newCategoryId)
            .WithIcon(Icon.Empty)
            .WithName("New Category")
            .Build();
        _localDatabase.GetCategories().Insert(newCategory);

        var hiddenTransaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "Hidden Transaction",
            TransactionDetails = new FiatDetails(_hiddenAccountId, 200, true)
        }.BuildDomainObject();
        _localDatabase.GetTransactions().Insert(hiddenTransaction.AsEntity());

        var visibleTransaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "Visible Transaction",
            TransactionDetails = new FiatDetails(_visibleAccountId, 200, true)
        }.BuildDomainObject();
        _localDatabase.GetTransactions().Insert(visibleTransaction.AsEntity());
    }

    [Test]
    public async Task BulkRename_HiddenAccountTransactions_Succeeds()
    {
        var handler = new BulkRenameTransactionsHandler(_transactionRepository);
        var transaction = _localDatabase.GetTransactions().FindOne(x => x.Name == "Hidden Transaction");

        var command = new BulkRenameTransactionsCommand
        {
            TransactionIds = new[] { transaction.Id.ToString() },
            NewName = "Renamed Hidden Transaction"
        };

        var result = await handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UpdatedCount, Is.EqualTo(1));

        var updated = _localDatabase.GetTransactions().FindById(transaction.Id);
        Assert.That(updated.Name, Is.EqualTo("Renamed Hidden Transaction"));
    }

    [Test]
    public async Task BulkChangeCategory_HiddenAccountTransactions_Succeeds()
    {
        var handler = new BulkChangeCategoryTransactionsHandler(_transactionRepository, _categoryRepository);
        var transaction = _localDatabase.GetTransactions().FindOne(x => x.Name == "Hidden Transaction");

        var command = new BulkChangeCategoryTransactionsCommand
        {
            TransactionIds = new[] { transaction.Id.ToString() },
            NewCategoryId = _newCategoryId.Value
        };

        var result = await handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UpdatedCount, Is.EqualTo(1));

        var updated = _localDatabase.GetTransactions().FindById(transaction.Id);
        Assert.That(updated.CategoryId.ToString(), Is.EqualTo(_newCategoryId.Value));
    }
}
