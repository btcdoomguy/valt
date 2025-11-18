using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Transactions;

[TestFixture]
public class TransactionTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fiatAccount = new FiatAccountBuilder()
        {
            Id = _fiatAccountId,
            Name = "Fiat Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Income")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);

        await base.SeedDatabase();
    }

    [Test]
    public Task Should_Reset_AutoSatAmount()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatDetails(_fiatAccountId, 153.32m, true),
            AutoSatAmountDetails =
                new AutoSatAmountDetails(true, SatAmountState.Processed, BtcValue.ParseSats(123456))
        }.BuildDomainObject();

        transaction.ChangeTransactionDetails(new FiatDetails(_fiatAccountId, 200.00m, true));

        Assert.That(transaction.AutoSatAmountDetails, Is.EqualTo(AutoSatAmountDetails.Pending));
        
        return Task.CompletedTask;
    }

    [Test]
    public async Task Should_Null_AutoSatAmount()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatDetails(_fiatAccountId, 153.32m, true),
            AutoSatAmountDetails =
                new AutoSatAmountDetails(true, SatAmountState.Processed, BtcValue.ParseSats(123456))
        }.BuildDomainObject();

        transaction.ChangeTransactionDetails(new BitcoinDetails(_fiatAccountId, BtcValue.ParseBitcoin(100000), true));

        Assert.That(transaction.AutoSatAmountDetails, Is.Null);
    }
}