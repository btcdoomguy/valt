using Microsoft.Extensions.DependencyInjection;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra;

namespace Valt.Tests.Domain.Budget.FixedExpenses;

public class FixedExpenseIntegrationTests : IntegrationTest
{
    private FixedExpenseId _fixedExpenseId = null!;
    private TransactionId _transactionId = null!;

    [SetUp]
    public async Task Setup()
    {
        var fixedExpenseRepo = _serviceProvider.GetRequiredService<IFixedExpenseRepository>();
        var fixedExpense =
            FixedExpense.New("Electricity", null, new CategoryId(), FiatCurrency.Brl,
                new List<FixedExpenseRange>()
                {
                  FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                      FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 30)  
                }, true);
        
        await fixedExpenseRepo.SaveFixedExpenseAsync(fixedExpense);
        _fixedExpenseId = fixedExpense.Id;

        //adds a transaction bound to the fixed expense entry
        var brlAccount = FiatAccount.New("My account", AccountCurrencyNickname.Empty, true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        var accountRepo = _serviceProvider.GetRequiredService<IAccountRepository>();

        await accountRepo.SaveAccountAsync(brlAccount);

        var transaction = Transaction.New(new DateOnly(2025, 1, 25), "Electricity", new CategoryId(),
            new FiatDetails(brlAccount.Id, FiatValue.New(170m), false), null,
            new TransactionFixedExpenseReference(_fixedExpenseId.Value, new DateOnly(2025, 1, 30)));
        var transactionRepo = _serviceProvider.GetRequiredService<ITransactionRepository>();

        await transactionRepo.SaveTransactionAsync(transaction);
        
        _transactionId = transaction.Id;
    }

    [Test]
    public async Task Should_Generate_FixedExpenseRecord()
    {
        var expectedDate = new DateOnly(2025, 1, 30).ToValtDateTime();
        var record = _localDatabase.GetFixedExpenseRecords().FindOne(x =>
            x.FixedExpense.Id == _fixedExpenseId.ToObjectId() &&
            x.Transaction.Id == _transactionId.ToObjectId() && x.ReferenceDate == expectedDate);

        Assert.That(record, Is.Not.Null);
    }

    [Test]
    public async Task Should_Generate_AndThen_Delete_FixedExpenseRecord_If_Transaction_Is_Deleted()
    {
        var transactionRepo = _serviceProvider.GetRequiredService<ITransactionRepository>();
        await transactionRepo.DeleteTransactionAsync(_transactionId);

        var expectedDate = new DateOnly(2025, 1, 30).ToValtDateTime();
        var record = _localDatabase.GetFixedExpenseRecords().FindOne(x =>
            x.FixedExpense.Id == _fixedExpenseId.ToObjectId() &&
            x.Transaction.Id == _transactionId.ToObjectId() && x.ReferenceDate == expectedDate);

        Assert.That(record, Is.Null);
    }

    [Test]
    public async Task Should_Generate_AndThen_Delete_FixedExpenseRecord_If_FixedExpense_Is_Deleted()
    {
        var fixedExpenseRepo = _serviceProvider.GetRequiredService<IFixedExpenseRepository>();
        await fixedExpenseRepo.DeleteFixedExpenseAsync(_fixedExpenseId);

        var expectedDate = new DateOnly(2025, 1, 30).ToValtDateTime();
        var record = _localDatabase.GetFixedExpenseRecords().FindOne(x =>
            x.FixedExpense.Id == _fixedExpenseId.ToObjectId() &&
            x.Transaction.Id == _transactionId.ToObjectId() && x.ReferenceDate == expectedDate);

        Assert.That(record, Is.Null);
    }

    [TearDown]
    public async Task ClearTables()
    {
        _localDatabase.GetFixedExpenseRecords().DeleteAll();
        _localDatabase.GetFixedExpenses().DeleteAll();
        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetAccounts().DeleteAll();
    }
}