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
public class TransactionsCalculator_BtcCreditDebtTests : DatabaseTest
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
                Value = 100000
            }
            .Build();

        _localDatabase.GetAccounts().Upsert(btcAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Common")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Upsert(category);

        await base.SeedDatabase();
    }

    [Test]
    public async Task Should_Sum_BtcTransactionsInAccount()
    {
        var credit100000 = Transaction.New(new DateOnly(2023, 1, 1), "Credit", _categoryId,
            new BitcoinDetails(_btcAccountId, 100000, true), "Hello", null);

        await _transactionRepository.SaveTransactionAsync(credit100000);

        var credit200000 = Transaction.New(new DateOnly(2023, 1, 2), "Credit", _categoryId,
            new BitcoinDetails(_btcAccountId, 200000, true), "Hello", null);

        await _transactionRepository.SaveTransactionAsync(credit200000);

        var debt50000 = Transaction.New(new DateOnly(2023, 1, 3), "Debt", _categoryId,
            new BitcoinDetails(_btcAccountId, 50000, false), "Hello", null);

        await _transactionRepository.SaveTransactionAsync(debt50000);

        var calculator = new AccountTotalsCalculator(_localDatabase, new AccountCacheService(_localDatabase, new Clock()));

        var total = await calculator.CalculateBtcTotalAsync(_btcAccountId);

        Assert.That(total.SatsTotal, Is.EqualTo(350000));
    }
}