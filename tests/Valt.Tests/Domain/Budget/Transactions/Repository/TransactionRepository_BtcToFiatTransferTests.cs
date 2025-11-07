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
public class TransactionRepository_BtcToFiatTransferTests : DatabaseTest
{
    private AccountId _fromBtcAccountId = null!;
    private AccountId _toFiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fromBtcAccountId = IdGenerator.Generate();
        _toFiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fromBtcAccount = new FiatAccountBuilder()
        {
            Id = _toFiatAccountId,
            Name = "Test Fiat",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1
        }.Build();

        _localDatabase.GetAccounts().Insert(fromBtcAccount);

        var toFiatAccount = new BtcAccountBuilder()
            {
                Id = _fromBtcAccountId,
                Name = "Btc Account 2",
                Value = 1
            }
            .Build();

        _localDatabase.GetAccounts().Insert(toFiatAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Conversion")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);

        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_StoreAndRetrieve_BtcToFiatTransfer()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new BitcoinToFiatDetails(_fromBtcAccountId, _toFiatAccountId, 720000, 1500)
        }.BuildDomainObject();

        var transactionDetails = (BitcoinToFiatDetails)transaction.TransactionDetails;

        await _transactionRepository.SaveTransactionAsync(transaction);

        Assert.That(transaction.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(restoredTransaction.TransactionDetails, Is.InstanceOf(typeof(BitcoinToFiatDetails)));
        var btcToFiatTransaction = (BitcoinToFiatDetails)restoredTransaction.TransactionDetails!;
        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction.Id, Is.EqualTo(transaction.Id));
            Assert.That(restoredTransaction.Date, Is.EqualTo(transaction.Date));
            Assert.That(btcToFiatTransaction.FromBtcAccountId, Is.EqualTo(transactionDetails.FromBtcAccountId));
            Assert.That(btcToFiatTransaction.ToFiatAccountId, Is.EqualTo(transactionDetails.ToFiatAccountId));
            Assert.That(btcToFiatTransaction.FromAmount, Is.EqualTo(transactionDetails.FromAmount));
            Assert.That(btcToFiatTransaction.ToAmount, Is.EqualTo(transactionDetails.ToAmount));
            Assert.That(btcToFiatTransaction.BtcPrice, Is.EqualTo(transactionDetails.BtcPrice));
        });
    }
}