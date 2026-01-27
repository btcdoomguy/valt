using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoals;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Goals.Queries;

[TestFixture]
public class GetGoalsHandlerTests : DatabaseTest
{
    private GetGoalsHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing goals from previous tests
        var existingGoals = await _goalRepository.GetAllAsync();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _handler = new GetGoalsHandler(_goalQueries);
    }

    [Test]
    public async Task HandleAsync_WithNoGoals_ReturnsEmptyList()
    {
        var query = new GetGoalsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_WithGoals_ReturnsAllGoals()
    {
        var goal1 = GoalBuilder.AStackBitcoinGoal(1_000_000).Build();
        var goal2 = GoalBuilder.ASpendingLimitGoal(500m).Build();
        await _goalRepository.SaveAsync(goal1);
        await _goalRepository.SaveAsync(goal2);

        var query = new GetGoalsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task HandleAsync_WithFilterDate_ReturnsGoalsForPeriod()
    {
        var januaryGoal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 1, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        var februaryGoal = GoalBuilder.ASpendingLimitGoal(500m)
            .WithRefDate(new DateOnly(2024, 2, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(januaryGoal);
        await _goalRepository.SaveAsync(februaryGoal);

        var query = new GetGoalsQuery { FilterDate = new DateOnly(2024, 1, 20) };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(januaryGoal.Id.Value));
    }

    [Test]
    public async Task HandleAsync_WithYearlyGoal_ReturnsGoalForEntireYear()
    {
        var yearlyGoal = GoalBuilder.AStackBitcoinGoal(10_000_000)
            .WithRefDate(new DateOnly(2024, 6, 1))
            .WithPeriod(GoalPeriods.Yearly)
            .Build();
        await _goalRepository.SaveAsync(yearlyGoal);

        var queryJanuary = new GetGoalsQuery { FilterDate = new DateOnly(2024, 1, 15) };
        var queryDecember = new GetGoalsQuery { FilterDate = new DateOnly(2024, 12, 25) };

        var resultJanuary = await _handler.HandleAsync(queryJanuary);
        var resultDecember = await _handler.HandleAsync(queryDecember);

        Assert.That(resultJanuary, Has.Count.EqualTo(1));
        Assert.That(resultDecember, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_MapsStackBitcoinGoalTypeCorrectly()
    {
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000).Build();
        await _goalRepository.SaveAsync(goal);

        var query = new GetGoalsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].GoalType, Is.TypeOf<StackBitcoinGoalTypeOutputDTO>());
        var goalType = (StackBitcoinGoalTypeOutputDTO)result[0].GoalType;
        Assert.That(goalType.TargetSats, Is.EqualTo(1_000_000));
    }

    [Test]
    public async Task HandleAsync_MapsReduceExpenseCategoryGoalTypeCorrectly()
    {
        var goal = GoalBuilder.AReduceExpenseCategoryGoal(500m, "cat-123", "Food").Build();
        await _goalRepository.SaveAsync(goal);

        var query = new GetGoalsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].GoalType, Is.TypeOf<ReduceExpenseCategoryGoalTypeOutputDTO>());
        var goalType = (ReduceExpenseCategoryGoalTypeOutputDTO)result[0].GoalType;
        Assert.Multiple(() =>
        {
            Assert.That(goalType.TargetAmount, Is.EqualTo(500m));
            Assert.That(goalType.CategoryId, Is.EqualTo("cat-123"));
            Assert.That(goalType.CategoryName, Is.EqualTo("Food"));
        });
    }

    [Test]
    public async Task HandleAsync_SortsGoalsByStateAndPeriod()
    {
        var openMonthly = GoalBuilder.AStackBitcoinGoal(100)
            .WithState(GoalStates.Open)
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        var openYearly = GoalBuilder.AStackBitcoinGoal(200)
            .WithState(GoalStates.Open)
            .WithPeriod(GoalPeriods.Yearly)
            .Build();
        var completed = GoalBuilder.AStackBitcoinGoal(300)
            .WithState(GoalStates.Completed)
            .Build();
        var failed = GoalBuilder.AStackBitcoinGoal(400)
            .WithState(GoalStates.Failed)
            .Build();

        await _goalRepository.SaveAsync(failed);
        await _goalRepository.SaveAsync(completed);
        await _goalRepository.SaveAsync(openYearly);
        await _goalRepository.SaveAsync(openMonthly);

        var query = new GetGoalsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].State, Is.EqualTo((int)GoalStates.Open));
            Assert.That(result[0].Period, Is.EqualTo((int)GoalPeriods.Monthly));
            Assert.That(result[1].State, Is.EqualTo((int)GoalStates.Open));
            Assert.That(result[1].Period, Is.EqualTo((int)GoalPeriods.Yearly));
            Assert.That(result[2].State, Is.EqualTo((int)GoalStates.Completed));
            Assert.That(result[3].State, Is.EqualTo((int)GoalStates.Failed));
        });
    }
}
