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
public class TransactionRepository_BtcToBtcTransferTests : DatabaseTest
{
    private AccountId _fromBtcAccountId = null!;
    private AccountId _toBtcAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fromBtcAccountId = IdGenerator.Generate();
        _toBtcAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var btcAccount1 = new BtcAccountBuilder()
            {
                Id = _fromBtcAccountId,
                Name = "Btc Account",
                Value = 1
            }
            .Build();

        _localDatabase.GetAccounts().Insert(btcAccount1);

        var btcAccount2 = new BtcAccountBuilder()
            {
                Id = _toBtcAccountId,
                Name = "Btc Account 2",
                Value = 1
            }
            .Build();
        _localDatabase.GetAccounts().Insert(btcAccount2);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Income")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);

        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_Store_And_Retrieve_BtcToBtcTransfer()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new BitcoinToBitcoinDetails(_fromBtcAccountId, _toBtcAccountId, 200000)
        }.BuildDomainObject();

        var transactionDetails = (BitcoinToBitcoinDetails)transaction.TransactionDetails;

        await _transactionRepository.SaveTransactionAsync(transaction);

        Assert.That(transaction.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(restoredTransaction.TransactionDetails, Is.InstanceOf(typeof(BitcoinToBitcoinDetails)));
        var btcToBtcTransaction = (BitcoinToBitcoinDetails)restoredTransaction.TransactionDetails!;
        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction.Id, Is.EqualTo(transaction.Id));
            Assert.That(restoredTransaction.Date, Is.EqualTo(transaction.Date));
            Assert.That(btcToBtcTransaction.FromBtcAccountId, Is.EqualTo(transactionDetails.FromBtcAccountId));
            Assert.That(btcToBtcTransaction.ToBtcAccountId, Is.EqualTo(transactionDetails.ToBtcAccountId));
            Assert.That(btcToBtcTransaction.Amount, Is.EqualTo(transactionDetails.Amount));
        });
    }
}