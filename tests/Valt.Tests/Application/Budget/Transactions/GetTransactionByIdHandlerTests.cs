using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.App.Modules.Budget.Transactions.Queries.GetTransactionById;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class GetTransactionByIdHandlerTests : DatabaseTest
{
    private GetTransactionByIdHandler _handler = null!;
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
        _handler = new GetTransactionByIdHandler(_transactionRepository);
    }

    [Test]
    public async Task HandleAsync_WithExistingTransaction_ReturnsDto()
    {
        var transaction = Transaction.New(
            new DateOnly(2024, 1, 15),
            TransactionName.New("Grocery Shopping"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(50m), false),
            "Some notes",
            null,
            null);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var query = new GetTransactionByIdQuery { TransactionId = transaction.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(transaction.Id.Value));
            Assert.That(result.Date, Is.EqualTo(new DateOnly(2024, 1, 15)));
            Assert.That(result.Name, Is.EqualTo("Grocery Shopping"));
            Assert.That(result.CategoryId, Is.EqualTo(_category.Id.Value));
            Assert.That(result.Notes, Is.EqualTo("Some notes"));
            Assert.That(result.GroupId, Is.Null);
            Assert.That(result.FixedExpenseReference, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_WithFiatTransaction_MapsFiatDetails()
    {
        var transaction = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Test"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(75.50m), true),
            null,
            null,
            null);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var query = new GetTransactionByIdQuery { TransactionId = transaction.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Details, Is.TypeOf<FiatTransactionDto>());
        var fiatDetails = (FiatTransactionDto)result.Details;
        Assert.Multiple(() =>
        {
            Assert.That(fiatDetails.FromAccountId, Is.EqualTo(_fiatAccount.Id.Value));
            Assert.That(fiatDetails.Amount, Is.EqualTo(75.50m));
            Assert.That(fiatDetails.IsCredit, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_WithGroupId_MapsGroupId()
    {
        var groupId = new GroupId();
        var transaction = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Installment"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(100m), false),
            null,
            null,
            groupId);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var query = new GetTransactionByIdQuery { TransactionId = transaction.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GroupId, Is.EqualTo(groupId.Value));
    }

    [Test]
    public async Task HandleAsync_WithDebitTransaction_MapsIsCredit()
    {
        var transaction = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Expense"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(100m), false),
            null,
            null,
            null);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var query = new GetTransactionByIdQuery { TransactionId = transaction.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        var fiatDetails = (FiatTransactionDto)result!.Details;
        Assert.That(fiatDetails.IsCredit, Is.False);
    }
}
