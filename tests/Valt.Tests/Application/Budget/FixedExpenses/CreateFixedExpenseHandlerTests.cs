using Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Tests.Application.Budget.FixedExpenses;

[TestFixture]
public class CreateFixedExpenseHandlerTests : DatabaseTest
{
    private CreateFixedExpenseHandler _handler = null!;
    private FiatAccount _fiatAccount = null!;
    private Category _category = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccount = FiatAccount.New(
            AccountName.New("Checking"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(1000m),
            null);
        await _accountRepository.SaveAccountAsync(_fiatAccount);

        _category = Category.New(CategoryName.New("Bills"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_category);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateFixedExpenseHandler(
            _fixedExpenseRepository,
            _categoryRepository,
            _accountRepository,
            new CreateFixedExpenseValidator());
    }

    [Test]
    public async Task HandleAsync_WithAccountAndFixedAmount_CreatesFixedExpense()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "Internet Bill",
            CategoryId = _category.Id.Value,
            DefaultAccountId = _fiatAccount.Id.Value,
            Enabled = true,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today),
                    FixedAmount = 50.00m,
                    PeriodId = 0, // Monthly
                    Day = 15
                }
            ]
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.FixedExpenseId, Is.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_WithCurrencyAndRangedAmount_CreatesFixedExpense()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "Electricity Bill",
            CategoryId = _category.Id.Value,
            Currency = "USD",
            Enabled = true,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today),
                    RangedAmountMin = 50.00m,
                    RangedAmountMax = 150.00m,
                    PeriodId = 0, // Monthly
                    Day = 20
                }
            ]
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "",
            CategoryId = _category.Id.Value,
            DefaultAccountId = _fiatAccount.Id.Value,
            Enabled = true,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today),
                    FixedAmount = 50.00m,
                    PeriodId = 0,
                    Day = 15
                }
            ]
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
    public async Task HandleAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "Test",
            CategoryId = "000000000000000000000001",
            DefaultAccountId = _fiatAccount.Id.Value,
            Enabled = true,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today),
                    FixedAmount = 50.00m,
                    PeriodId = 0,
                    Day = 15
                }
            ]
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAccount_ReturnsNotFound()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "Test",
            CategoryId = _category.Id.Value,
            DefaultAccountId = "000000000000000000000001",
            Enabled = true,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today),
                    FixedAmount = 50.00m,
                    PeriodId = 0,
                    Day = 15
                }
            ]
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNT_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithInvalidCurrency_ReturnsError()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "Test",
            CategoryId = _category.Id.Value,
            Currency = "INVALID",
            Enabled = true,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today),
                    FixedAmount = 50.00m,
                    PeriodId = 0,
                    Day = 15
                }
            ]
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_CURRENCY"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNoAccountOrCurrency_ReturnsValidationError()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "Test",
            CategoryId = _category.Id.Value,
            Enabled = true,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today),
                    FixedAmount = 50.00m,
                    PeriodId = 0,
                    Day = 15
                }
            ]
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyRanges_ReturnsValidationError()
    {
        var command = new CreateFixedExpenseCommand
        {
            Name = "Test",
            CategoryId = _category.Id.Value,
            DefaultAccountId = _fiatAccount.Id.Value,
            Enabled = true,
            Ranges = []
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
