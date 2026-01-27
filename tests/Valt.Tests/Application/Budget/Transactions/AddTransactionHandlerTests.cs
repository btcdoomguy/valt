using LiteDB;
using Valt.App.Modules.Budget.Transactions.Commands.AddTransaction;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class AddTransactionHandlerTests : DatabaseTest
{
    private AddTransactionHandler _handler = null!;
    private FiatAccount _fiatAccount = null!;
    private BtcAccount _btcAccount = null!;
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

        _btcAccount = BtcAccount.New(
            AccountName.New("Bitcoin Wallet"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            (BtcValue)100000L,
            null);
        await _accountRepository.SaveAccountAsync(_btcAccount);

        _category = Category.New(CategoryName.New("Food"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_category);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new AddTransactionHandler(
            _transactionRepository,
            _categoryRepository,
            _accountRepository,
            _fixedExpenseRepository,
            new AddTransactionValidator());
    }

    [Test]
    public async Task HandleAsync_WithFiatDebt_CreatesTransaction()
    {
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Grocery Shopping",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 50.00m,
                IsCredit = false
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.TransactionId, Is.Not.Empty);
        });

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(
            new TransactionId(result.Value!.TransactionId));
        Assert.That(savedTransaction, Is.Not.Null);
        Assert.That(savedTransaction!.Name.Value, Is.EqualTo("Grocery Shopping"));
        Assert.That(savedTransaction.TransactionDetails, Is.TypeOf<FiatDetails>());

        var details = (FiatDetails)savedTransaction.TransactionDetails;
        Assert.Multiple(() =>
        {
            Assert.That(details.Amount.Value, Is.EqualTo(50.00m));
            Assert.That(details.Credit, Is.False);
        });
    }

    [Test]
    public async Task HandleAsync_WithFiatCredit_CreatesTransaction()
    {
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Salary",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 5000.00m,
                IsCredit = true
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(
            new TransactionId(result.Value!.TransactionId));
        var details = (FiatDetails)savedTransaction!.TransactionDetails;
        Assert.That(details.Credit, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithBitcoinTransaction_CreatesTransaction()
    {
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Bitcoin Purchase",
            CategoryId = _category.Id.Value,
            Details = new BitcoinTransactionDto
            {
                FromAccountId = _btcAccount.Id.Value,
                AmountSats = 10000,
                IsCredit = true
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(
            new TransactionId(result.Value!.TransactionId));
        Assert.That(savedTransaction!.TransactionDetails, Is.TypeOf<BitcoinDetails>());
    }

    [Test]
    public async Task HandleAsync_WithFiatToFiatTransfer_CreatesTransaction()
    {
        // Create a second fiat account
        var savingsAccount = FiatAccount.New(
            AccountName.New("Savings"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(0m),
            null);
        await _accountRepository.SaveAccountAsync(savingsAccount);

        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Transfer to Savings",
            CategoryId = _category.Id.Value,
            Details = new FiatToFiatTransferDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                ToAccountId = savingsAccount.Id.Value,
                FromAmount = 100.00m,
                ToAmount = 100.00m
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(
            new TransactionId(result.Value!.TransactionId));
        Assert.That(savedTransaction!.TransactionDetails, Is.TypeOf<FiatToFiatDetails>());
    }

    [Test]
    public async Task HandleAsync_WithNotes_SavesNotes()
    {
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Test Transaction",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 25.00m,
                IsCredit = false
            },
            Notes = "This is a test note"
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(
            new TransactionId(result.Value!.TransactionId));
        Assert.That(savedTransaction!.Notes, Is.EqualTo("This is a test note"));
    }

    [Test]
    public async Task HandleAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Test",
            CategoryId = "000000000000000000000001",
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 10.00m,
                IsCredit = false
            }
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
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Test",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = "000000000000000000000001",
                Amount = 10.00m,
                IsCredit = false
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNT_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 10.00m,
                IsCredit = false
            }
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
    public async Task HandleAsync_WithZeroAmount_ReturnsValidationError()
    {
        var command = new AddTransactionCommand
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Test",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 0m,
                IsCredit = false
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
