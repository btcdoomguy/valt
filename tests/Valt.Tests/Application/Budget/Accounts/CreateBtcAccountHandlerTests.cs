using Valt.App.Modules.Budget.Accounts.Commands.CreateBtcAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class CreateBtcAccountHandlerTests : DatabaseTest
{
    private CreateBtcAccountHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateBtcAccountHandler(
            _accountRepository,
            _accountGroupRepository,
            new CreateBtcAccountValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidCommand_CreatesAccount()
    {
        var command = new CreateBtcAccountCommand
        {
            Name = "Cold Storage",
            InitialAmountSats = 100000000, // 1 BTC
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.AccountId, Is.Not.Empty);
        });

        var savedAccount = await _accountRepository.GetAccountByIdAsync(new AccountId(result.Value!.AccountId));
        Assert.That(savedAccount, Is.Not.Null);
        Assert.That(savedAccount, Is.TypeOf<BtcAccount>());

        var btcAccount = (BtcAccount)savedAccount!;
        Assert.Multiple(() =>
        {
            Assert.That(btcAccount.Name.Value, Is.EqualTo("Cold Storage"));
            Assert.That(btcAccount.InitialAmount.Sats, Is.EqualTo(100000000));
            Assert.That(btcAccount.Visible, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_WithCurrencyNickname_CreatesAccountWithNickname()
    {
        var command = new CreateBtcAccountCommand
        {
            Name = "Bitcoin Wallet",
            CurrencyNickname = "BTC",
            InitialAmountSats = 0,
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedAccount = await _accountRepository.GetAccountByIdAsync(new AccountId(result.Value!.AccountId));
        var btcAccount = (BtcAccount)savedAccount!;
        Assert.That(btcAccount.CurrencyNickname.Value, Is.EqualTo("BTC"));
    }

    [Test]
    public async Task HandleAsync_WithValidGroup_CreatesAccountInGroup()
    {
        // Create group first
        var group = AccountGroup.New(AccountGroupName.New("Crypto"));
        await _accountGroupRepository.SaveAsync(group);

        var command = new CreateBtcAccountCommand
        {
            Name = "Lightning Wallet",
            InitialAmountSats = 50000,
            Visible = true,
            IconId = Icon.Empty.ToString(),
            GroupId = group.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedAccount = await _accountRepository.GetAccountByIdAsync(new AccountId(result.Value!.AccountId));
        Assert.That(savedAccount!.GroupId?.Value, Is.EqualTo(group.Id.Value));
    }

    [Test]
    public async Task HandleAsync_WithNonExistentGroup_ReturnsNotFound()
    {
        var command = new CreateBtcAccountCommand
        {
            Name = "Orphan Account",
            InitialAmountSats = 0,
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
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateBtcAccountCommand
        {
            Name = "",
            InitialAmountSats = 0,
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
    public async Task HandleAsync_WithEmptyIcon_ReturnsValidationError()
    {
        var command = new CreateBtcAccountCommand
        {
            Name = "Valid Name",
            InitialAmountSats = 0,
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
        var command = new CreateBtcAccountCommand
        {
            Name = new string('A', 31), // Max is 30
            InitialAmountSats = 0,
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
        var command = new CreateBtcAccountCommand
        {
            Name = "Valid Name",
            CurrencyNickname = new string('A', 16), // Max is 15
            InitialAmountSats = 0,
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
    public async Task HandleAsync_WithZeroInitialAmount_Succeeds()
    {
        var command = new CreateBtcAccountCommand
        {
            Name = "Empty Wallet",
            InitialAmountSats = 0,
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedAccount = await _accountRepository.GetAccountByIdAsync(new AccountId(result.Value!.AccountId));
        var btcAccount = (BtcAccount)savedAccount!;
        Assert.That(btcAccount.InitialAmount.Sats, Is.EqualTo(0));
    }
}
