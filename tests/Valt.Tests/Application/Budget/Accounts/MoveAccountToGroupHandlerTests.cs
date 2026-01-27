using Valt.App.Modules.Budget.Accounts.Commands.MoveAccountToGroup;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class MoveAccountToGroupHandlerTests : DatabaseTest
{
    private MoveAccountToGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing accounts and groups from previous tests
        var existingAccounts = await _accountRepository.GetAccountsAsync();
        foreach (var account in existingAccounts)
            await _accountRepository.DeleteAccountAsync(account);

        var existingGroups = await _accountGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _accountGroupRepository.DeleteAsync(group.Id);

        _handler = new MoveAccountToGroupHandler(_accountRepository, _accountGroupRepository);
    }

    [Test]
    public async Task HandleAsync_MovesAccountToExistingGroup()
    {
        var group = AccountGroup.New(AccountGroupName.New("Savings"));
        await _accountGroupRepository.SaveAsync(group);

        var account = FiatAccount.New(
            AccountName.New("Account"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(100m),
            null);
        await _accountRepository.SaveAccountAsync(account);

        var command = new MoveAccountToGroupCommand
        {
            AccountId = account.Id.Value,
            TargetGroupId = group.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAccount = await _accountRepository.GetAccountByIdAsync(account.Id);
        Assert.That(updatedAccount!.GroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public async Task HandleAsync_RemovesAccountFromGroup_WhenTargetGroupIsEmpty()
    {
        var group = AccountGroup.New(AccountGroupName.New("Initial Group"));
        await _accountGroupRepository.SaveAsync(group);

        var account = FiatAccount.New(
            AccountName.New("Account"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(100m),
            group.Id);
        await _accountRepository.SaveAccountAsync(account);

        var command = new MoveAccountToGroupCommand
        {
            AccountId = account.Id.Value,
            TargetGroupId = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAccount = await _accountRepository.GetAccountByIdAsync(account.Id);
        Assert.That(updatedAccount!.GroupId, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyAccountId_ReturnsValidationError()
    {
        var command = new MoveAccountToGroupCommand
        {
            AccountId = "",
            TargetGroupId = "some-group-id"
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
        var command = new MoveAccountToGroupCommand
        {
            AccountId = "000000000000000000000001",
            TargetGroupId = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNT_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentGroupId_ReturnsGroupNotFound()
    {
        var account = FiatAccount.New(
            AccountName.New("Account"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(100m),
            null);
        await _accountRepository.SaveAccountAsync(account);

        var command = new MoveAccountToGroupCommand
        {
            AccountId = account.Id.Value,
            TargetGroupId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GROUP_NOT_FOUND"));
        });
    }
}
