using NSubstitute;
using Valt.App.Modules.Budget.Categories.Commands.DeleteCategory;
using Valt.App.Modules.Budget.Transactions.Contracts;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Tests.Application.Budget.Categories;

[TestFixture]
public class DeleteCategoryHandlerTests : DatabaseTest
{
    private DeleteCategoryHandler _handler = null!;
    private ITransactionQueries _transactionQueries = null!;
    private Category _existingCategory = null!;

    protected override async Task SeedDatabase()
    {
        _existingCategory = Category.New(CategoryName.New("ToDelete"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_existingCategory);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _transactionQueries = Substitute.For<ITransactionQueries>();
        _transactionQueries.GetTransactionsAsync(Arg.Any<TransactionQueryFilter>())
            .Returns(new TransactionsDTO([]));

        _handler = new DeleteCategoryHandler(_categoryRepository, _transactionQueries);
    }

    [Test]
    public async Task HandleAsync_WithValidCategoryNotInUse_DeletesCategory()
    {
        var command = new DeleteCategoryCommand
        {
            CategoryId = _existingCategory.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var deletedCategory = await _categoryRepository.GetCategoryByIdAsync(_existingCategory.Id);
        Assert.That(deletedCategory, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        var command = new DeleteCategoryCommand
        {
            CategoryId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithCategoryInUse_ReturnsError()
    {
        // Setup mock to return transactions using this category
        _transactionQueries.GetTransactionsAsync(Arg.Any<TransactionQueryFilter>())
            .Returns(new TransactionsDTO([
                new TransactionDTO
                {
                    Id = "tx1",
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    Name = "Test Transaction",
                    CategoryId = _existingCategory.Id.Value,
                    CategoryName = "ToDelete",
                    FromAccountId = "acc1",
                    FromAccountName = "Test Account",
                    TransferType = "Fiat",
                    TransactionType = "Expense",
                    AutoSatAmountSummary = ""
                }
            ]));

        var command = new DeleteCategoryCommand
        {
            CategoryId = _existingCategory.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_IN_USE"));
        });

        // Verify category was not deleted
        var category = await _categoryRepository.GetCategoryByIdAsync(_existingCategory.Id);
        Assert.That(category, Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyCategoryId_ReturnsValidationError()
    {
        var command = new DeleteCategoryCommand
        {
            CategoryId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
