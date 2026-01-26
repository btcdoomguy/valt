using LiteDB;
using Valt.App.Modules.Budget.Transactions.Commands.DeleteTransactionGroup;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class DeleteTransactionGroupHandlerTests : DatabaseTest
{
    private DeleteTransactionGroupHandler _handler = null!;
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

        _category = Category.New(CategoryName.New("Shopping"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_category);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new DeleteTransactionGroupHandler(_transactionRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidGroupId_DeletesAllGroupTransactions()
    {
        var groupId = new GroupId();

        // Create 3 transactions in the same group (installments)
        for (var i = 0; i < 3; i++)
        {
            var transaction = Transaction.New(
                DateOnly.FromDateTime(DateTime.Today).AddMonths(i),
                TransactionName.New($"Installment {i + 1}/3"),
                _category.Id,
                new FiatDetails(_fiatAccount.Id, FiatValue.New(100m), false),
                null,
                null,
                groupId);
            await _transactionRepository.SaveTransactionAsync(transaction);
        }

        var command = new DeleteTransactionGroupCommand { GroupId = groupId.Value };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.DeletedCount, Is.EqualTo(3));

        // Verify all transactions were deleted
        var remaining = await _transactionRepository.GetTransactionsByGroupIdAsync(groupId);
        Assert.That(remaining.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task HandleAsync_WithEmptyGroupId_ReturnsValidationError()
    {
        var command = new DeleteTransactionGroupCommand { GroupId = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentGroupId_ReturnsGroupNotFound()
    {
        var command = new DeleteTransactionGroupCommand { GroupId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GROUP_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotAffectTransactionsInOtherGroups()
    {
        var groupId1 = new GroupId();
        var groupId2 = new GroupId();

        // Create transactions in two different groups
        var tx1 = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Group 1 Transaction"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(100m), false),
            null,
            null,
            groupId1);
        await _transactionRepository.SaveTransactionAsync(tx1);

        var tx2 = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Group 2 Transaction"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(200m), false),
            null,
            null,
            groupId2);
        await _transactionRepository.SaveTransactionAsync(tx2);

        // Delete only group 1
        var command = new DeleteTransactionGroupCommand { GroupId = groupId1.Value };
        await _handler.HandleAsync(command);

        // Verify group 2 still exists
        var group2Transactions = await _transactionRepository.GetTransactionsByGroupIdAsync(groupId2);
        Assert.That(group2Transactions.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_DoesNotAffectUngroupedTransactions()
    {
        var groupId = new GroupId();

        // Create a grouped transaction
        var groupedTx = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Grouped"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(100m), false),
            null,
            null,
            groupId);
        await _transactionRepository.SaveTransactionAsync(groupedTx);

        // Create an ungrouped transaction
        var ungroupedTx = Transaction.New(
            DateOnly.FromDateTime(DateTime.Today),
            TransactionName.New("Ungrouped"),
            _category.Id,
            new FiatDetails(_fiatAccount.Id, FiatValue.New(50m), false),
            null,
            null,
            null);
        await _transactionRepository.SaveTransactionAsync(ungroupedTx);

        // Delete the group
        var command = new DeleteTransactionGroupCommand { GroupId = groupId.Value };
        await _handler.HandleAsync(command);

        // Verify ungrouped transaction still exists
        var retrieved = await _transactionRepository.GetTransactionByIdAsync(ungroupedTx.Id);
        Assert.That(retrieved, Is.Not.Null);
    }
}
