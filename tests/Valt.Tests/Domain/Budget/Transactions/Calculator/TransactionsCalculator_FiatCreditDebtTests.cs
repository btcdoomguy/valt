using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Budget.Accounts.Services;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Transactions.Calculator;

[TestFixture]
public class TransactionsCalculator_FiatCreditDebtTests : DatabaseTest
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
            Value = 1000
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Common")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);

        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_Sum_FiatTransactionsInAccount()
    {
        var trans1 = Transaction.New(new DateOnly(2023, 1, 1),
            "Credit",
            _categoryId,
            new FiatDetails(_fiatAccountId, 100, true),
            "Hello", null);

        await _transactionRepository.SaveTransactionAsync(trans1);

        var trans2 = Transaction.New(new DateOnly(2023, 1, 2),
            "Credit",
            _categoryId,
            new FiatDetails(_fiatAccountId, 200, true),
            "Hello", null);

        await _transactionRepository.SaveTransactionAsync(trans2);

        var trans3 = Transaction.New(new DateOnly(2023, 1, 3),
            "Debit",
            _categoryId,
            new FiatDetails(_fiatAccountId, 50, false),
            "Hello", null);

        await _transactionRepository.SaveTransactionAsync(trans3);

        var calculator = new AccountTotalsCalculator(_localDatabase, new AccountCacheService(_localDatabase, new Clock()));

        var total = await calculator.CalculateFiatTotalAsync(_fiatAccountId);

        Assert.That(total.FiatTotal, Is.EqualTo(1250m));
    }
}