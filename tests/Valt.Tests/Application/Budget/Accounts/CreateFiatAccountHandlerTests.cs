using Valt.App.Modules.Budget.Accounts.Commands.CreateFiatAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class CreateFiatAccountHandlerTests : DatabaseTest
{
    private CreateFiatAccountHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateFiatAccountHandler(
            _accountRepository,
            _accountGroupRepository,
            new CreateFiatAccountValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidCommand_CreatesAccount()
    {
        var command = new CreateFiatAccountCommand
        {
            Name = "Checking",
            Currency = "USD",
            InitialAmount = 1000m,
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
        Assert.That(savedAccount, Is.TypeOf<FiatAccount>());

        var fiatAccount = (FiatAccount)savedAccount!;
        Assert.Multiple(() =>
        {
            Assert.That(fiatAccount.Name.Value, Is.EqualTo("Checking"));
            Assert.That(fiatAccount.FiatCurrency, Is.EqualTo(FiatCurrency.Usd));
            Assert.That(fiatAccount.InitialAmount.Value, Is.EqualTo(1000m));
            Assert.That(fiatAccount.Visible, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_WithCurrencyNickname_CreatesAccountWithNickname()
    {
        var command = new CreateFiatAccountCommand
        {
            Name = "Euro Savings",
            Currency = "EUR",
            CurrencyNickname = "EU",
            InitialAmount = 500m,
            Visible = true,
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedAccount = await _accountRepository.GetAccountByIdAsync(new AccountId(result.Value!.AccountId));
        var fiatAccount = (FiatAccount)savedAccount!;
        Assert.That(fiatAccount.CurrencyNickname.Value, Is.EqualTo("EU"));
    }

    [Test]
    public async Task HandleAsync_WithValidGroup_CreatesAccountInGroup()
    {
        // Create group first
        var group = AccountGroup.New(AccountGroupName.New("Banking"));
        await _accountGroupRepository.SaveAsync(group);

        var command = new CreateFiatAccountCommand
        {
            Name = "Savings",
            Currency = "USD",
            InitialAmount = 0m,
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
        var command = new CreateFiatAccountCommand
        {
            Name = "Orphan Account",
            Currency = "USD",
            InitialAmount = 0m,
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
        var command = new CreateFiatAccountCommand
        {
            Name = "Invalid Currency Account",
            Currency = "XXX",
            InitialAmount = 0m,
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
        var command = new CreateFiatAccountCommand
        {
            Name = "",
            Currency = "USD",
            InitialAmount = 0m,
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
    public async Task HandleAsync_WithEmptyCurrency_ReturnsValidationError()
    {
        var command = new CreateFiatAccountCommand
        {
            Name = "Valid Name",
            Currency = "",
            InitialAmount = 0m,
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
        var command = new CreateFiatAccountCommand
        {
            Name = "Valid Name",
            Currency = "USD",
            InitialAmount = 0m,
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
        var command = new CreateFiatAccountCommand
        {
            Name = new string('A', 31), // Max is 30
            Currency = "USD",
            InitialAmount = 0m,
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
        var command = new CreateFiatAccountCommand
        {
            Name = "Valid Name",
            Currency = "USD",
            CurrencyNickname = new string('A', 16), // Max is 15
            InitialAmount = 0m,
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
