using Valt.App.Modules.Budget.Accounts.Commands.MoveAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class MoveAccountHandlerTests : DatabaseTest
{
    private MoveAccountHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing accounts from previous tests
        var existingAccounts = await _accountRepository.GetAccountsAsync();
        foreach (var account in existingAccounts)
            await _accountRepository.DeleteAccountAsync(account);

        _handler = new MoveAccountHandler(_accountRepository);
    }

    [Test]
    public async Task HandleAsync_MovesAccountToNewPosition()
    {
        // Create 3 accounts with initial order
        var account1 = FiatAccount.New(AccountName.New("Account 1"), AccountCurrencyNickname.Empty, true, Icon.Empty, FiatCurrency.Usd, FiatValue.New(100m), null);
        account1.ChangeDisplayOrder(0);
        var account2 = FiatAccount.New(AccountName.New("Account 2"), AccountCurrencyNickname.Empty, true, Icon.Empty, FiatCurrency.Usd, FiatValue.New(200m), null);
        account2.ChangeDisplayOrder(1);
        var account3 = FiatAccount.New(AccountName.New("Account 3"), AccountCurrencyNickname.Empty, true, Icon.Empty, FiatCurrency.Usd, FiatValue.New(300m), null);
        account3.ChangeDisplayOrder(2);

        await _accountRepository.SaveAccountAsync(account1);
        await _accountRepository.SaveAccountAsync(account2);
        await _accountRepository.SaveAccountAsync(account3);

        // Move account 1 to position 2
        var command = new MoveAccountCommand
        {
            AccountId = account1.Id.Value,
            NewDisplayOrder = 2
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        // Verify the new order
        var allAccounts = (await _accountRepository.GetAccountsAsync())
            .Where(a => a.GroupId == null)
            .OrderBy(a => a.DisplayOrder)
            .ToList();
        Assert.That(allAccounts[0].Name.Value, Is.EqualTo("Account 2"));
        Assert.That(allAccounts[1].Name.Value, Is.EqualTo("Account 3"));
        Assert.That(allAccounts[2].Name.Value, Is.EqualTo("Account 1"));
    }

    [Test]
    public async Task HandleAsync_WithEmptyAccountId_ReturnsValidationError()
    {
        var command = new MoveAccountCommand
        {
            AccountId = "",
            NewDisplayOrder = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAccountId_ReturnsNotFound()
    {
        var command = new MoveAccountCommand
        {
            AccountId = "000000000000000000000001",
            NewDisplayOrder = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNT_NOT_FOUND"));
        });
    }
}
