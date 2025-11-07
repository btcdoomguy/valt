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
public class TransactionsCalculator_FiatTransferTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;
    private AccountId _fiatAccountId2 = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();
        _fiatAccountId2 = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fiatAccount = new FiatAccountBuilder()
        {
            Id = _fiatAccountId,
            Name = "Fiat Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1000m
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount);

        var fiatAccount2 = new FiatAccountBuilder()
        {
            Id = _fiatAccountId2,
            Name = "Fiat Account2",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1000
        }.Build();
        _localDatabase.GetAccounts().Insert(fiatAccount2);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Common")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);


        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_Sum_TransferToAnotherAccount()
    {
        var transfer100 = Transaction.New(new DateOnly(2023, 1, 1),
            "Transfer",
            _categoryId,
            new FiatToFiatDetails(_fiatAccountId, _fiatAccountId2, 100, 100),
            "Hello", null);

        await _transactionRepository.SaveTransactionAsync(transfer100);

        var calculator = new AccountTotalsCalculator(_localDatabase, new AccountCacheService(_localDatabase, new Clock()));

        var totalAccount1 = await calculator.CalculateFiatTotalAsync(_fiatAccountId);

        Assert.That(totalAccount1.FiatTotal, Is.EqualTo(900m));

        var totalAccount2 = await calculator.CalculateFiatTotalAsync(_fiatAccountId2);

        Assert.That(totalAccount2.FiatTotal, Is.EqualTo(1100m));
    }
}