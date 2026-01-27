using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Services;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.Accounts.Queries;

[TestFixture]
public class GetAccountsQueryHandlerTests : DatabaseTest
{
    private AccountId _btcAccountId = null!;
    private AccountId _fiatAccountId = null!;

    protected override async Task SeedDatabase()
    {
        _btcAccountId = IdGenerator.Generate();
        _fiatAccountId = IdGenerator.Generate();

        var existingBtcAccount = new BtcAccountBuilder()
            {
                Id = _btcAccountId,
                Name = "Btc Account",
                Value = BtcValue.New(100000)
            }
            .Build();

        _localDatabase.GetAccounts().Insert(existingBtcAccount);

        var existingFiatAccount = new FiatAccountBuilder()
            {
                Id = _fiatAccountId,
                Name = "Fiat Account",
                Value = FiatValue.New(1000m),
                FiatCurrency = FiatCurrency.Brl
            }
            .Build();

        _localDatabase.GetAccounts().Insert(existingFiatAccount);
    }

    [Test]
    public async Task Should_Get_InitialTotals()
    {
        var query = new AccountQueries(_localDatabase, new AccountTotalsCalculator(_localDatabase, new AccountCacheService(_localDatabase, new Clock())));

        var result = await query.GetAccountSummariesAsync(false);

        var btcAccountDto = result.Items.SingleOrDefault(x => x.Id == _btcAccountId);
        var fiatAccountDto = result.Items.SingleOrDefault(x => x.Id == _fiatAccountId);
        Assert.That(result.Items.Count, Is.EqualTo(2));
        Assert.That(btcAccountDto.SatsTotal, Is.EqualTo(100000));
        Assert.That(fiatAccountDto.FiatTotal, Is.EqualTo(1000m));
    }
}