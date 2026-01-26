using Valt.App.Modules.Budget.Transactions.Queries.HasTransactionsForAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class HasTransactionsForAccountHandlerTests : DatabaseTest
{
    private HasTransactionsForAccountHandler _handler = null!;
    private FiatAccount _fiatAccount = null!;
    private FiatAccount _emptyAccount = null!;
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

        _emptyAccount = FiatAccount.New(
            AccountName.New("Empty Account"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(0m),
            null);
        await _accountRepository.SaveAccountAsync(_emptyAccount);

        _category = Category.New(CategoryName.New("Food"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_category);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new HasTransactionsForAccountHandler(_transactionQueries);
    }

    [Test]
    public async Task HandleAsync_WithAccountHavingTransactions_ReturnsTrue()
    {
        var transaction = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Test"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(50m), false),
            null,
            null,
            null);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var query = new HasTransactionsForAccountQuery { AccountId = _fiatAccount.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithAccountHavingNoTransactions_ReturnsFalse()
    {
        var query = new HasTransactionsForAccountQuery { AccountId = _emptyAccount.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HandleAsync_WithMultipleTransactions_ReturnsTrue()
    {
        for (var i = 0; i < 3; i++)
        {
            var transaction = Transaction.New(
                DateOnly.FromDateTime(DateTime.Today).AddDays(i),
                TransactionName.New($"Test {i}"),
                _category.Id,
                new FiatDetails(_fiatAccount.Id, FiatValue.New(10m), false),
                null,
                null,
                null);
            await _transactionRepository.SaveTransactionAsync(transaction);
        }

        var query = new HasTransactionsForAccountQuery { AccountId = _fiatAccount.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAccountId_ReturnsFalse()
    {
        var query = new HasTransactionsForAccountQuery { AccountId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.False);
    }
}
