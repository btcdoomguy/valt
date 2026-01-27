using LiteDB;
using Valt.App.Modules.Budget.Transactions.Commands.DeleteTransaction;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class DeleteTransactionHandlerTests : DatabaseTest
{
    private DeleteTransactionHandler _handler = null!;
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
        _handler = new DeleteTransactionHandler(_transactionRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidTransactionId_DeletesTransaction()
    {
        // Create a transaction first
        var transaction = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Test"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(50m), false),
            null,
            null,
            null);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var command = new DeleteTransactionCommand
        {
            TransactionId = transaction.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        // Verify deletion by checking the database directly (bypassing repository bug)
        var entity = _localDatabase.GetTransactions().FindById(new ObjectId(transaction.Id.Value));
        Assert.That(entity, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyTransactionId_ReturnsValidationError()
    {
        var command = new DeleteTransactionCommand
        {
            TransactionId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
