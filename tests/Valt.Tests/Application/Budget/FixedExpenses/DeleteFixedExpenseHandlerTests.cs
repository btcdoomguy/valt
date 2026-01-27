using LiteDB;
using Valt.App.Modules.Budget.FixedExpenses.Commands.DeleteFixedExpense;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Budget.FixedExpenses;

[TestFixture]
public class DeleteFixedExpenseHandlerTests : DatabaseTest
{
    private DeleteFixedExpenseHandler _handler = null!;
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
        _handler = new DeleteFixedExpenseHandler(_fixedExpenseRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidFixedExpenseId_DeletesFixedExpense()
    {
        // Create a fixed expense first
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(_fiatAccount.Id)
            .WithCategoryId(_category.Id)
            .WithName("Internet Bill")
            .WithFixedAmountRange(50m, FixedExpensePeriods.Monthly, DateOnly.FromDateTime(DateTime.Today), 15)
            .BuildDomainObject();
        await _fixedExpenseRepository.SaveFixedExpenseAsync(fixedExpense);

        var command = new DeleteFixedExpenseCommand
        {
            FixedExpenseId = fixedExpense.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        // Verify deletion
        var entity = _localDatabase.GetFixedExpenses().FindById(new ObjectId(fixedExpense.Id.Value));
        Assert.That(entity, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyFixedExpenseId_ReturnsValidationError()
    {
        var command = new DeleteFixedExpenseCommand
        {
            FixedExpenseId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentFixedExpenseId_ReturnsNotFound()
    {
        var command = new DeleteFixedExpenseCommand
        {
            FixedExpenseId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("FIXED_EXPENSE_NOT_FOUND"));
        });
    }
}
