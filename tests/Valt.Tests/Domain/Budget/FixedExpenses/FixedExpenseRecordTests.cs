using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.FixedExpenses;

public class FixedExpenseRecordTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;
    private CategoryId _categoryId = null!;
    private FixedExpenseId _fixedExpenseId = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();
        _fixedExpenseId = IdGenerator.Generate();

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

        var fixedExpense = new FixedExpenseBuilder()
        {
            Id = _fixedExpenseId,
            Name = "Test",
            CategoryId = new CategoryId(category.Id.ToString()),
            Currency = FiatCurrency.Brl,
            Enabled = true,
            Ranges = new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateFixedAmount(FiatValue.New(1000m), FixedExpensePeriods.Monthly, new DateOnly(2020, 1, 1), 5)
            },
            Version = 1
        }.Build();
        
        _localDatabase.GetFixedExpenses().Insert(fixedExpense);
        
        await base.SeedDatabase();
    }
    
    [Test]
    public async Task Should_Produce_FixedExpenseReferenceBound_Event()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatDetails(_fiatAccountId, 153.32m, false),
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            FixedExpense = new TransactionFixedExpenseReference(new FixedExpenseId(), new DateOnly(2023, 1, 1))
        }.BuildDomainObject();
        var transactionDetails = (FiatDetails)transaction.TransactionDetails;

        await _transactionRepository.SaveTransactionAsync(transaction);

        Assert.That(transaction.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionBoundToFixedExpenseEvent>());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(restoredTransaction.TransactionDetails, Is.InstanceOf(typeof(FiatDetails)));
        var fiatRestoredTransaction = (FiatDetails)restoredTransaction.TransactionDetails!;
        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction.Id, Is.EqualTo(transaction.Id));
            Assert.That(restoredTransaction.Date, Is.EqualTo(transaction.Date));
            Assert.That(fiatRestoredTransaction.FiatAccountId, Is.EqualTo(transactionDetails.FiatAccountId));
            Assert.That(fiatRestoredTransaction.Amount, Is.EqualTo(transactionDetails.Amount));
            Assert.That(fiatRestoredTransaction.Credit, Is.EqualTo(transactionDetails.Credit));
        });
    }
    
    [Test]
    public async Task Should_Produce_FixedExpenseReferenceUnbound_Event()
    {
        var transaction = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "My Transaction",
            TransactionDetails = new FiatDetails(_fiatAccountId, 153.32m, false),
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            FixedExpense = new TransactionFixedExpenseReference(new FixedExpenseId(), new DateOnly(2023, 1, 1))
        }.BuildDomainObject();
        
        transaction.SetFixedExpense(null);
        
        await _transactionRepository.SaveTransactionAsync(transaction);
        
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<TransactionUnboundFromFixedExpenseEvent>());
    }
}