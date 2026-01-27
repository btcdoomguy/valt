using Valt.App.Modules.Budget.Accounts.Commands.EditAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class EditAccountHandlerTests : DatabaseTest
{
    private EditAccountHandler _handler = null!;
    private FiatAccount _fiatAccount = null!;
    private BtcAccount _btcAccount = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccount = FiatAccount.New(
            AccountName.New("Original Fiat"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(1000m),
            null);
        await _accountRepository.SaveAccountAsync(_fiatAccount);

        _btcAccount = BtcAccount.New(
            AccountName.New("Original BTC"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            (BtcValue)100000L,
            null);
        await _accountRepository.SaveAccountAsync(_btcAccount);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new EditAccountHandler(
            _accountRepository,
            _accountGroupRepository,
            new EditAccountValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidFiatCommand_UpdatesAccount()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = "Updated Checking",
            Currency = "EUR",
            InitialAmountFiat = 2000m,
            Visible = false,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAccount = await _accountRepository.GetAccountByIdAsync(_fiatAccount.Id);
        var fiatAccount = (FiatAccount)updatedAccount!;
        Assert.Multiple(() =>
        {
            Assert.That(fiatAccount.Name.Value, Is.EqualTo("Updated Checking"));
            Assert.That(fiatAccount.FiatCurrency, Is.EqualTo(FiatCurrency.Eur));
            Assert.That(fiatAccount.InitialAmount.Value, Is.EqualTo(2000m));
            Assert.That(fiatAccount.Visible, Is.False);
        });
    }

    [Test]
    public async Task HandleAsync_WithValidBtcCommand_UpdatesAccount()
    {
        var command = new EditAccountCommand
        {
            AccountId = _btcAccount.Id.Value,
            Name = "Updated Cold Storage",
            InitialAmountSats = 200000L,
            Visible = false,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAccount = await _accountRepository.GetAccountByIdAsync(_btcAccount.Id);
        var btcAccount = (BtcAccount)updatedAccount!;
        Assert.Multiple(() =>
        {
            Assert.That(btcAccount.Name.Value, Is.EqualTo("Updated Cold Storage"));
            Assert.That(btcAccount.InitialAmount.Sats, Is.EqualTo(200000L));
            Assert.That(btcAccount.Visible, Is.False);
        });
    }

    [Test]
    public async Task HandleAsync_WithCurrencyNickname_UpdatesNickname()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = "Savings",
            CurrencyNickname = "Dollars",
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAccount = await _accountRepository.GetAccountByIdAsync(_fiatAccount.Id);
        Assert.That(updatedAccount!.CurrencyNickname.Value, Is.EqualTo("Dollars"));
    }

    [Test]
    public async Task HandleAsync_AssignToGroup_UpdatesGroupId()
    {
        // Create group first
        var group = AccountGroup.New(AccountGroupName.New("Banking"));
        await _accountGroupRepository.SaveAsync(group);

        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = _fiatAccount.Name.Value,
            Visible = true,
            IconId = Icon.Empty.ToString(),
            GroupId = group.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAccount = await _accountRepository.GetAccountByIdAsync(_fiatAccount.Id);
        Assert.That(updatedAccount!.GroupId?.Value, Is.EqualTo(group.Id.Value));
    }

    [Test]
    public async Task HandleAsync_RemoveFromGroup_ClearsGroupId()
    {
        // First assign to group
        var group = AccountGroup.New(AccountGroupName.New("Banking"));
        await _accountGroupRepository.SaveAsync(group);
        _fiatAccount.AssignToGroup(group.Id);
        await _accountRepository.SaveAccountAsync(_fiatAccount);

        // Then remove from group
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = _fiatAccount.Name.Value,
            Visible = true,
            IconId = Icon.Empty.ToString(),
            GroupId = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAccount = await _accountRepository.GetAccountByIdAsync(_fiatAccount.Id);
        Assert.That(updatedAccount!.GroupId, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAccount_ReturnsNotFound()
    {
        var command = new EditAccountCommand
        {
            AccountId = "000000000000000000000001",
            Name = "Any Name",
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNT_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentGroup_ReturnsNotFound()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = "Valid Name",
            Visible = true,
            IconId = Icon.Empty.ToString(),
            GroupId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNTGROUP_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithInvalidCurrency_ReturnsError()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = "Valid Name",
            Currency = "XXX",
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_CURRENCY"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = "",
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
            Assert.That(result.Error.HasValidationErrors, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyAccountId_ReturnsValidationError()
    {
        var command = new EditAccountCommand
        {
            AccountId = "",
            Name = "Valid Name",
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyIcon_ReturnsValidationError()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = "Valid Name",
            Visible = true,
            IconId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNameTooLong_ReturnsValidationError()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = new string('A', 31), // Max is 30
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNicknameTooLong_ReturnsValidationError()
    {
        var command = new EditAccountCommand
        {
            AccountId = _fiatAccount.Id.Value,
            Name = "Valid Name",
            CurrencyNickname = new string('A', 16), // Max is 15
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
