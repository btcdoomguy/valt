using Valt.App.Modules.Budget.Accounts.Commands.DeleteAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class DeleteAccountHandlerTests : DatabaseTest
{
    private DeleteAccountHandler _handler = null!;
    private FiatAccount _fiatAccount = null!;
    private BtcAccount _btcAccount = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccount = FiatAccount.New(
            AccountName.New("Test Fiat"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(1000m),
            null);
        await _accountRepository.SaveAccountAsync(_fiatAccount);

        _btcAccount = BtcAccount.New(
            AccountName.New("Test BTC"),
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
        _handler = new DeleteAccountHandler(_accountRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidFiatAccountId_DeletesAccount()
    {
        // Create a fresh account for this test to avoid interference from other tests
        var accountToDelete = FiatAccount.New(
            AccountName.New("Account To Delete"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(500m),
            null);
        await _accountRepository.SaveAccountAsync(accountToDelete);

        var command = new DeleteAccountCommand
        {
            AccountId = accountToDelete.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var deletedAccount = await _accountRepository.GetAccountByIdAsync(accountToDelete.Id);
        Assert.That(deletedAccount, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithValidBtcAccountId_DeletesAccount()
    {
        // Create a fresh account for this test to avoid interference from other tests
        var accountToDelete = BtcAccount.New(
            AccountName.New("BTC Account To Delete"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            (BtcValue)50000L,
            null);
        await _accountRepository.SaveAccountAsync(accountToDelete);

        var command = new DeleteAccountCommand
        {
            AccountId = accountToDelete.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var deletedAccount = await _accountRepository.GetAccountByIdAsync(accountToDelete.Id);
        Assert.That(deletedAccount, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAccount_ReturnsNotFound()
    {
        var command = new DeleteAccountCommand
        {
            AccountId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNT_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyAccountId_ReturnsValidationError()
    {
        var command = new DeleteAccountCommand
        {
            AccountId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithWhitespaceAccountId_ReturnsValidationError()
    {
        var command = new DeleteAccountCommand
        {
            AccountId = "   "
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithAccountThatHasTransactions_ReturnsError()
    {
        // Create a fresh account for this test
        var accountWithTransactions = FiatAccount.New(
            AccountName.New("Account With Transactions"),
            AccountCurrencyNickname.Empty,
            true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.New(1000m),
            null);
        await _accountRepository.SaveAccountAsync(accountWithTransactions);

        // Create a category first
        var category = Category.New(CategoryName.New("Food"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(category);

        // Create a transaction linked to the account
        var transaction = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Test Transaction"),
            category.Id,
            new FiatDetails(accountWithTransactions.Id, FiatValue.New(100m), false),
            null,
            null,
            null);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var command = new DeleteAccountCommand
        {
            AccountId = accountWithTransactions.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ACCOUNT_HAS_TRANSACTIONS"));
        });

        // Verify account was not deleted
        var account = await _accountRepository.GetAccountByIdAsync(accountWithTransactions.Id);
        Assert.That(account, Is.Not.Null);
    }
}
