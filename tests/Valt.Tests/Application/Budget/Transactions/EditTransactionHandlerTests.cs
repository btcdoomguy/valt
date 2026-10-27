using Valt.App.Modules.Budget.Transactions.Commands.EditTransaction;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class EditTransactionHandlerTests : DatabaseTest
{
    private EditTransactionHandler _handler = null!;
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

        _category = Category.New(CategoryName.New("Food"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_category);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new EditTransactionHandler(
            _transactionRepository,
            _categoryRepository,
            _accountRepository,
            _fixedExpenseRepository,
            new EditTransactionValidator());
    }

    [Test]
    public async Task HandleAsync_WithIsExcludedTrue_SetsFlagOnTransaction()
    {
        var transaction = TransactionBuilder.ATransaction()
            .WithCategoryId(_category.Id)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("Grocery Shopping")
            .WithTransactionDetails(new FiatDetails(_fiatAccount.Id, 50m, false))
            .BuildDomainObject();
        await _transactionRepository.SaveTransactionAsync(transaction);

        var command = new EditTransactionCommand
        {
            TransactionId = transaction.Id.Value,
            Date = new DateOnly(2023, 1, 1),
            Name = "Grocery Shopping",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 50.00m,
                IsCredit = false
            },
            IsExcluded = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(savedTransaction!.IsExcluded, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithIsExcludedFalse_ClearsFlagOnExcludedTransaction()
    {
        var transaction = TransactionBuilder.ATransaction()
            .WithCategoryId(_category.Id)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("Internal Transfer")
            .WithTransactionDetails(new FiatDetails(_fiatAccount.Id, 50m, false))
            .WithExcluded(true)
            .BuildDomainObject();
        await _transactionRepository.SaveTransactionAsync(transaction);

        var command = new EditTransactionCommand
        {
            TransactionId = transaction.Id.Value,
            Date = new DateOnly(2023, 1, 1),
            Name = "Internal Transfer",
            CategoryId = _category.Id.Value,
            Details = new FiatTransactionDto
            {
                FromAccountId = _fiatAccount.Id.Value,
                Amount = 50.00m,
                IsCredit = false
            },
            IsExcluded = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(savedTransaction!.IsExcluded, Is.False);
    }
}
