using LiteDB;
using Valt.App.Modules.Goals.Commands.CreateGoal;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Tests.Application.Goals;

[TestFixture]
public class CreateGoalHandlerTests : DatabaseTest
{
    private CreateGoalHandler _handler = null!;
    private Category _category = null!;

    protected override async Task SeedDatabase()
    {
        _category = Category.New(CategoryName.New("Food"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_category);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateGoalHandler(
            _goalRepository,
            _categoryRepository,
            new CreateGoalValidator());
    }

    [Test]
    public async Task HandleAsync_WithStackBitcoinGoal_CreatesGoal()
    {
        var command = new CreateGoalCommand
        {
            RefDate = new DateOnly(2024, 1, 15),
            Period = 0, // Monthly
            GoalType = new StackBitcoinGoalTypeDTO { TargetSats = 1_000_000 }
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.GoalId, Is.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_WithSpendingLimitGoal_CreatesGoal()
    {
        var command = new CreateGoalCommand
        {
            RefDate = new DateOnly(2024, 1, 15),
            Period = 0, // Monthly
            GoalType = new SpendingLimitGoalTypeDTO { TargetAmount = 500m }
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithDcaGoal_CreatesGoal()
    {
        var command = new CreateGoalCommand
        {
            RefDate = new DateOnly(2024, 1, 15),
            Period = 1, // Yearly
            GoalType = new DcaGoalTypeDTO { TargetPurchaseCount = 52 }
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithReduceExpenseCategoryGoal_CreatesGoal()
    {
        var command = new CreateGoalCommand
        {
            RefDate = new DateOnly(2024, 1, 15),
            Period = 0, // Monthly
            GoalType = new ReduceExpenseCategoryGoalTypeDTO
            {
                TargetAmount = 200m,
                CategoryId = _category.Id.Value
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithInvalidPeriod_ReturnsValidationError()
    {
        var command = new CreateGoalCommand
        {
            RefDate = new DateOnly(2024, 1, 15),
            Period = 5, // Invalid
            GoalType = new StackBitcoinGoalTypeDTO { TargetSats = 1_000_000 }
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithZeroTargetSats_ReturnsValidationError()
    {
        var command = new CreateGoalCommand
        {
            RefDate = new DateOnly(2024, 1, 15),
            Period = 0,
            GoalType = new StackBitcoinGoalTypeDTO { TargetSats = 0 }
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        var command = new CreateGoalCommand
        {
            RefDate = new DateOnly(2024, 1, 15),
            Period = 0,
            GoalType = new ReduceExpenseCategoryGoalTypeDTO
            {
                TargetAmount = 200m,
                CategoryId = "000000000000000000000001"
            }
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_NOT_FOUND"));
        });
    }
}
