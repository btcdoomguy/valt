using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Events;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Accounts;

[TestFixture]
public class AccountRepositoryUpdateFiatAccountTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;

    protected override Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();

        var fiatAccount = new FiatAccountBuilder()
            {
                Id = _fiatAccountId,
                Name = "Fiat Account",
                Icon = Icon.Empty,
                FiatCurrency = FiatCurrency.Brl,
                Value = 1
            }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount);
        return Task.CompletedTask;
    }

    [Test]
    public async Task SaveAccount_Should_Update_FiatAccount_Properly()
    {
        var repository = new AccountRepository(_localDatabase, _domainEventPublisher);

        var account = FiatAccount.Create(_fiatAccountId, "Fiat Account", true, Icon.Empty, FiatCurrency.Brl, 1, 0, 1);

        account.ChangeInitialAmount(2);

        await repository.SaveAccountAsync(account);

        Assert.That(account.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AccountUpdatedEvent>());
        var restoredAccount = await repository.GetAccountByIdAsync(account.Id);
        Assert.That(restoredAccount, Is.InstanceOf(typeof(FiatAccount)));
        var fiatRestoredAccount = (FiatAccount)restoredAccount;
        Assert.That(fiatRestoredAccount.Id, Is.EqualTo(account.Id));
        Assert.That(fiatRestoredAccount.Icon, Is.EqualTo(account.Icon));
        Assert.That(fiatRestoredAccount.Name, Is.EqualTo(account.Name));
        Assert.That(fiatRestoredAccount.Visible, Is.EqualTo(account.Visible));
        Assert.That(fiatRestoredAccount.InitialAmount, Is.EqualTo(account.InitialAmount));
    }
}