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
public class TransactionRepository_FiatToFiatTransferTests : DatabaseTest
{
    private AccountId _fromFiatAccountId = null!;
    private AccountId _toFiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fromFiatAccountId = IdGenerator.Generate();
        _toFiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fiatAccount1 = new FiatAccountBuilder()
        {
            Id = _fromFiatAccountId,
            Name = "Test Fiat",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount1);

        var fiatAccount2 = new FiatAccountBuilder()
        {
            Id = _toFiatAccountId,
            Name = "Test Fiat 2",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Usd,
            Value = 1
        }.Build();
        _localDatabase.GetAccounts().Insert(fiatAccount2);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Income")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);


        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_StoreAndRetrieve_FiatToFiatTransfer()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatToFiatDetails(_fromFiatAccountId, _toFiatAccountId, 100, 20),
            AutoSatAmountDetails = AutoSatAmountDetails.Pending
        }.BuildDomainObject();
        var transactionDetails = (FiatToFiatDetails)transaction.TransactionDetails;

        await _transactionRepository.SaveTransactionAsync(transaction);

        Assert.That(transaction.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(restoredTransaction.TransactionDetails, Is.InstanceOf(typeof(FiatToFiatDetails)));
        var fiatToFiatTransaction = (FiatToFiatDetails)restoredTransaction.TransactionDetails!;
        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction.Id, Is.EqualTo(transaction.Id));
            Assert.That(restoredTransaction.Date, Is.EqualTo(transaction.Date));
            Assert.That(fiatToFiatTransaction.FromFiatAccountId, Is.EqualTo(transactionDetails.FromFiatAccountId));
            Assert.That(fiatToFiatTransaction.ToFiatAccountId, Is.EqualTo(transactionDetails.ToFiatAccountId));
            Assert.That(fiatToFiatTransaction.FromAmount, Is.EqualTo(transactionDetails.FromAmount));
            Assert.That(fiatToFiatTransaction.ToAmount, Is.EqualTo(transactionDetails.ToAmount));
        });
    }
}