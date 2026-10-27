using Valt.App.Modules.Budget.Transactions.Commands.SetTransactionExclusion;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.Transactions;

[TestFixture]
public class SetTransactionExclusionHandlerTests : DatabaseTest
{
    private SetTransactionExclusionHandler _handler = null!;
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
        _handler = new SetTransactionExclusionHandler(
            _transactionRepository,
            new SetTransactionExclusionValidator());
    }

    [Test]
    public async Task HandleAsync_WithIsExcludedTrue_PersistsFlag()
    {
        var transaction = TransactionBuilder.ATransaction()
            .WithCategoryId(_category.Id)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("Internal Transfer")
            .WithTransactionDetails(new FiatDetails(_fiatAccount.Id, 50m, false))
            .BuildDomainObject();
        await _transactionRepository.SaveTransactionAsync(transaction);

        var command = new SetTransactionExclusionCommand
        {
            TransactionId = transaction.Id.Value,
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

        var command = new SetTransactionExclusionCommand
        {
            TransactionId = transaction.Id.Value,
            IsExcluded = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedTransaction = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(savedTransaction!.IsExcluded, Is.False);
    }

    [Test]
    public async Task HandleAsync_WithNonexistentTransactionId_ReturnsNotFound()
    {
        var command = new SetTransactionExclusionCommand
        {
            TransactionId = "00000000000000000000ffff",
            IsExcluded = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error!.Code, Is.EqualTo("TRANSACTION_NOT_FOUND"));
    }

    [Test]
    public void Validate_WithBlankTransactionId_IsInvalid()
    {
        var validator = new SetTransactionExclusionValidator();

        var validation = validator.Validate(new SetTransactionExclusionCommand
        {
            TransactionId = "   ",
            IsExcluded = true
        });

        Assert.That(validation.IsValid, Is.False);
    }
}
