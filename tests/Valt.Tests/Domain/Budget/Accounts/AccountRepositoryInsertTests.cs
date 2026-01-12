using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Events;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Tests.Domain.Budget.Accounts;

[TestFixture]
public class AccountRepositoryInsertTests : DatabaseTest
{
    [Test]
    public async Task SaveAccount_Should_Store_And_Retrieve_New_BtcAccount_Properly()
    {
        var repository = new AccountRepository(_localDatabase, _domainEventPublisher);

        var account = BtcAccount.New("Test", AccountCurrencyNickname.Empty, true, Icon.Empty, 0);

        await repository.SaveAccountAsync(account);

        Assert.That(account.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AccountCreatedEvent>());

        var restoredAccount = await repository.GetAccountByIdAsync(account.Id);
        Assert.That(restoredAccount, Is.InstanceOf(typeof(BtcAccount)));
        var btcRestoredAccount = (BtcAccount)restoredAccount;
        Assert.That(btcRestoredAccount.Id, Is.EqualTo(account.Id));
        Assert.That(btcRestoredAccount.Icon, Is.EqualTo(account.Icon));
        Assert.That(btcRestoredAccount.Name, Is.EqualTo(account.Name));
        Assert.That(btcRestoredAccount.CurrencyNickname, Is.EqualTo(account.CurrencyNickname));
        Assert.That(btcRestoredAccount.Visible, Is.EqualTo(account.Visible));
        Assert.That(btcRestoredAccount.InitialAmount, Is.EqualTo(account.InitialAmount));
    }

    [Test]
    public async Task SaveAccount_Should_Store_And_Retrieve_New_FiatAccount_Properly()
    {
        var repository = new AccountRepository(_localDatabase, _domainEventPublisher);

        var account = FiatAccount.New("Test", AccountCurrencyNickname.Empty, true, Icon.Empty, FiatCurrency.Brl, 0);

        await repository.SaveAccountAsync(account);

        Assert.That(account.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AccountCreatedEvent>());

        var restoredAccount = await repository.GetAccountByIdAsync(account.Id);
        Assert.That(restoredAccount, Is.InstanceOf(typeof(FiatAccount)));
        var fiatRestoredAccount = (FiatAccount)restoredAccount;
        Assert.That(fiatRestoredAccount.Id, Is.EqualTo(account.Id));
        Assert.That(fiatRestoredAccount.Icon, Is.EqualTo(account.Icon));
        Assert.That(fiatRestoredAccount.Name, Is.EqualTo(account.Name));
        Assert.That(fiatRestoredAccount.CurrencyNickname, Is.EqualTo(account.CurrencyNickname));
        Assert.That(fiatRestoredAccount.Visible, Is.EqualTo(account.Visible));
        Assert.That(fiatRestoredAccount.InitialAmount, Is.EqualTo(account.InitialAmount));
    }

    [Test]
    public async Task SaveAccount_Should_Store_And_Retrieve_BtcAccount_With_CurrencyNickname()
    {
        var repository = new AccountRepository(_localDatabase, _domainEventPublisher);

        AccountCurrencyNickname currencyNickname = "Lightning";
        var account = BtcAccount.New("Test", currencyNickname, true, Icon.Empty, 0);

        await repository.SaveAccountAsync(account);

        var restoredAccount = await repository.GetAccountByIdAsync(account.Id);
        Assert.That(restoredAccount, Is.InstanceOf(typeof(BtcAccount)));
        var btcRestoredAccount = (BtcAccount)restoredAccount;
        Assert.That(btcRestoredAccount.CurrencyNickname, Is.EqualTo(currencyNickname));
        Assert.That(btcRestoredAccount.CurrencyNickname.Value, Is.EqualTo("Lightning"));
    }

    [Test]
    public async Task SaveAccount_Should_Store_And_Retrieve_FiatAccount_With_CurrencyNickname()
    {
        var repository = new AccountRepository(_localDatabase, _domainEventPublisher);

        AccountCurrencyNickname currencyNickname = "USDT Solana";
        var account = FiatAccount.New("Test", currencyNickname, true, Icon.Empty, FiatCurrency.Usd, 0);

        await repository.SaveAccountAsync(account);

        var restoredAccount = await repository.GetAccountByIdAsync(account.Id);
        Assert.That(restoredAccount, Is.InstanceOf(typeof(FiatAccount)));
        var fiatRestoredAccount = (FiatAccount)restoredAccount;
        Assert.That(fiatRestoredAccount.CurrencyNickname, Is.EqualTo(currencyNickname));
        Assert.That(fiatRestoredAccount.CurrencyNickname.Value, Is.EqualTo("USDT Solana"));
    }
}