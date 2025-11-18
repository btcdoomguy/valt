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
public class TransactionRepository_BtcTransactionTests : DatabaseTest
{
    private AccountId _btcAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _btcAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();


        var btcAccount = new BtcAccountBuilder()
            {
                Id = _btcAccountId,
                Name = "Btc Account",
                Value = 1
            }
            .Build();

        _localDatabase.GetAccounts().Insert(btcAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Income")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);


        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_Store_And_Retrieve_BtcTransaction()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new BitcoinDetails(_btcAccountId, 200000, true)
        }.BuildDomainObject();

        var transactionDetails = (BitcoinDetails)transaction.TransactionDetails;

        await _transactionRepository.SaveTransactionAsync(transaction);

        Assert.That(transaction.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(restoredTransaction.TransactionDetails, Is.InstanceOf(typeof(BitcoinDetails)));
        var btcRestoredTransaction = (BitcoinDetails)restoredTransaction.TransactionDetails!;
        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction.Id, Is.EqualTo(transaction.Id));
            Assert.That(restoredTransaction.Date, Is.EqualTo(transaction.Date));
            Assert.That(btcRestoredTransaction.BtcAccountId, Is.EqualTo(transactionDetails.BtcAccountId));
            Assert.That(btcRestoredTransaction.Amount, Is.EqualTo(transactionDetails.Amount));
            Assert.That(btcRestoredTransaction.Credit, Is.EqualTo(transactionDetails.Credit));
        });
    }
}