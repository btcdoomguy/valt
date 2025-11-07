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
public class TransactionsCalculator_BtcTransferTests : DatabaseTest
{
    private AccountId _btcAccountId = null!;
    private AccountId _btcAccountId2 = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _btcAccountId = IdGenerator.Generate();
        _btcAccountId2 = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var btcAccount = new BtcAccountBuilder()
            {
                Id = _btcAccountId,
                Name = "Btc Account",
                Value = 100000
            }
            .Build();

        _localDatabase.GetAccounts().Insert(btcAccount);

        var btcAccount2 = new BtcAccountBuilder()
            {
                Id = _btcAccountId2,
                Name = "Btc Account2",
                Value = 100000
            }
            .Build();
        _localDatabase.GetAccounts().Insert(btcAccount2);

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
        var transfer50000 = Transaction.New(new DateOnly(2023, 1, 1),
            "Transfer",
            _categoryId,
            new BitcoinToBitcoinDetails(_btcAccountId, _btcAccountId2, 50000),
            "Hello", null);

        await _transactionRepository.SaveTransactionAsync(transfer50000);

        var calculator = new AccountTotalsCalculator(_localDatabase, new AccountCacheService(_localDatabase, new Clock()));

        var totalAccount1 = await calculator.CalculateBtcTotalAsync(_btcAccountId);

        Assert.That(totalAccount1.SatsTotal, Is.EqualTo(50000));

        var totalAccount2 = await calculator.CalculateBtcTotalAsync(_btcAccountId2);

        Assert.That(totalAccount2.SatsTotal, Is.EqualTo(150000));
    }
}