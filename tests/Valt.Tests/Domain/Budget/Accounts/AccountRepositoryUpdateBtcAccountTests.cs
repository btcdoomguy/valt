using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Events;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Accounts;

[TestFixture]
public class AccountRepositoryUpdateBtcAccountTests : DatabaseTest
{
    private AccountId _btcAccountId = null!;

    protected override Task SeedDatabase()
    {
        _btcAccountId = IdGenerator.Generate();

        var btcAccount = new BtcAccountBuilder()
        {
            Id = _btcAccountId,
            Name = "Btc Account",
            Icon = Icon.Empty,
            Value = 1
        }.Build();

        _localDatabase.GetAccounts().Insert(btcAccount);
        return Task.CompletedTask;
    }

    [Test]
    public async Task SaveAccount_Should_Update_BtcAccount_Properly()
    {
        var repository = new AccountRepository(_localDatabase, _domainEventPublisher);

        var account = BtcAccount.Create(_btcAccountId, "Btc Account", AccountCurrencyNickname.Empty, true, Icon.Empty, 1, 0, null, 1);

        account.ChangeInitialAmount(2);

        await repository.SaveAccountAsync(account);

        Assert.That(account.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AccountUpdatedEvent>());
        var restoredAccount = await repository.GetAccountByIdAsync(account.Id);
        Assert.That(restoredAccount, Is.InstanceOf(typeof(BtcAccount)));
        var btcRestoredAccount = (BtcAccount)restoredAccount;
        Assert.That(btcRestoredAccount.Id, Is.EqualTo(account.Id));
        Assert.That(btcRestoredAccount.Icon, Is.EqualTo(account.Icon));
        Assert.That(btcRestoredAccount.Name, Is.EqualTo(account.Name));
        Assert.That(btcRestoredAccount.Visible, Is.EqualTo(account.Visible));
        Assert.That(btcRestoredAccount.InitialAmount, Is.EqualTo(account.InitialAmount));
    }
}