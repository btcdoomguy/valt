using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using Valt.Tests.Builders;

namespace Valt.Tests.UseCases.Budget.Transactions.Queries;

[TestFixture]
public class GetTransactionsQueryHandlerTests : DatabaseTest
{
    private AccountId _btcAccountId = null!;
    private AccountId _fiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _btcAccountId = IdGenerator.Generate();
        _fiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var existingBtcAccount = new BtcAccountBuilder()
        {
            Id = _btcAccountId,
            Name = "Btc Account",
            Icon = Icon.Empty,
            Value = BtcValue.New(100000)
        }.Build();

        _localDatabase.GetAccounts().Insert(existingBtcAccount);

        var existingFiatAccount = new FiatAccountBuilder()
        {
            Id = _fiatAccountId,
            Name = "Fiat Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1000m
        }.Build();

        _localDatabase.GetAccounts().Insert(existingFiatAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithIcon(Icon.Empty)
            .WithName("Test")
            .Build();

        _localDatabase.GetCategories().Insert(category);

        var transaction1 = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatDetails(_fiatAccountId, 200, true)
        }.BuildDomainObject();

        _localDatabase.GetTransactions().Insert(transaction1.AsEntity());

        var transaction2 = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatDetails(_fiatAccountId, 200, true)
        }.BuildDomainObject();

        _localDatabase.GetTransactions().Insert(transaction2.AsEntity());
    }

    [Test]
    public async Task Should_Get_Transactions()
    {
        var filter = new TransactionQueryFilter()
        {
            From = new DateOnly(2023, 1, 1),
            To = new DateOnly(2023, 1, 31),
            AccountIds = new[] { _fiatAccountId.Value }
        };

        var query = new TransactionQueries(_localDatabase);

        var result = await query.GetTransactionsAsync(filter);

        Assert.That(result.Items.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Should_Get_NoTransactions_For_DateRangeWithoutTransactions()
    {
        var filter = new TransactionQueryFilter()
        {
            From = new DateOnly(2022, 1, 1),
            To = new DateOnly(2022, 1, 31),
            AccountIds = new[] { _fiatAccountId.Value }
        };

        var query = new TransactionQueries(_localDatabase);

        var result = await query.GetTransactionsAsync(filter);

        Assert.That(result.Items.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task Should_Get_NoTransactions_For_AccountWithoutTransactions()
    {
        var filter = new TransactionQueryFilter()
        {
            From = new DateOnly(2023, 1, 1),
            To = new DateOnly(2023, 1, 31),
            AccountIds = new[] { _btcAccountId.Value }
        };

        var query = new TransactionQueries(_localDatabase);

        var result = await query.GetTransactionsAsync(filter);

        Assert.That(result.Items.Count, Is.EqualTo(0));
    }
}