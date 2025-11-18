using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Transactions.Repository;

[TestFixture]
public class TransactionRepository_FiatToBtcTransferTests : DatabaseTest
{
    private AccountId _fromFiatAccountId = null!;
    private AccountId _toBtcAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fromFiatAccountId = IdGenerator.Generate();
        _toBtcAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fromFiatAccount = new FiatAccountBuilder()
        {
            Id = _fromFiatAccountId,
            Name = "Test Fiat",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1
        }.Build();

        _localDatabase.GetAccounts().Insert(fromFiatAccount);

        var btcAccount = new BtcAccountBuilder()
            {
                Id = _toBtcAccountId,
                Name = "Btc Account2",
                Value = 1
            }
            .Build();

        _localDatabase.GetAccounts().Insert(btcAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Conversion")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);


        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_StoreAndRetrieve_FiatToBtcTransfer()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatToBitcoinDetails(_fromFiatAccountId, _toBtcAccountId, 720000, 1500)
        }.BuildDomainObject();
        var transactionDetails = (FiatToBitcoinDetails)transaction.TransactionDetails;

        await _transactionRepository.SaveTransactionAsync(transaction);

        Assert.That(transaction.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(restoredTransaction.TransactionDetails, Is.InstanceOf(typeof(FiatToBitcoinDetails)));
        var fiatToBtcTransaction = (FiatToBitcoinDetails)restoredTransaction.TransactionDetails!;
        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction.Id, Is.EqualTo(transaction.Id));
            Assert.That(restoredTransaction.Date, Is.EqualTo(transaction.Date));
            Assert.That(fiatToBtcTransaction.FromFiatAccountId, Is.EqualTo(transactionDetails.FromFiatAccountId));
            Assert.That(fiatToBtcTransaction.ToBtcAccountId, Is.EqualTo(transactionDetails.ToBtcAccountId));
            Assert.That(fiatToBtcTransaction.FromAmount, Is.EqualTo(transactionDetails.FromAmount));
            Assert.That(fiatToBtcTransaction.ToAmount, Is.EqualTo(transactionDetails.ToAmount));
            Assert.That(fiatToBtcTransaction.BtcPrice, Is.EqualTo(transactionDetails.BtcPrice));
        });
    }
}