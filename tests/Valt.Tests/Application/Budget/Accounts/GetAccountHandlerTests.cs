using Valt.App.Modules.Budget.Accounts.Queries.GetAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class GetAccountHandlerTests : DatabaseTest
{
    private GetAccountHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing accounts from previous tests
        var existingAccounts = await _accountRepository.GetAccountsAsync();
        foreach (var account in existingAccounts)
            await _accountRepository.DeleteAccountAsync(account);

        _handler = new GetAccountHandler(_accountQueries);
    }

    [Test]
    public async Task HandleAsync_WithExistingFiatAccount_ReturnsDto()
    {
        var account = FiatAccount.New(
            AccountName.New("Checking"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(1000m),
            null);
        await _accountRepository.SaveAccountAsync(account);

        var query = new GetAccountQuery { AccountId = account.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(account.Id.Value));
            Assert.That(result.Name, Is.EqualTo("Checking"));
            Assert.That(result.Currency, Is.EqualTo("USD"));
            Assert.That(result.IsBtcAccount, Is.False);
            Assert.That(result.Visible, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_WithExistingBtcAccount_ReturnsBtcDto()
    {
        var account = BtcAccount.New(
            AccountName.New("Bitcoin Wallet"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            (BtcValue)100000L,
            null);
        await _accountRepository.SaveAccountAsync(account);

        var query = new GetAccountQuery { AccountId = account.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(account.Id.Value));
            Assert.That(result.Name, Is.EqualTo("Bitcoin Wallet"));
            Assert.That(result.IsBtcAccount, Is.True);
            Assert.That(result.InitialAmountSats, Is.EqualTo(100000L));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAccount_ReturnsNull()
    {
        var query = new GetAccountQuery { AccountId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithHiddenAccount_ReturnsWithVisibleFalse()
    {
        var account = FiatAccount.New(
            AccountName.New("Hidden"),
            AccountCurrencyNickname.Empty,
            false,
            Icon.Empty,
            FiatCurrency.Eur,
            FiatValue.New(500m),
            null);
        await _accountRepository.SaveAccountAsync(account);

        var query = new GetAccountQuery { AccountId = account.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Visible, Is.False);
    }
}
