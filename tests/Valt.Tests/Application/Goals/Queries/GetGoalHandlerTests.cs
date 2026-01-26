using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoal;
using Valt.Core.Modules.Goals;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Goals.Queries;

[TestFixture]
public class GetGoalHandlerTests : DatabaseTest
{
    private GetGoalHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing goals from previous tests
        var existingGoals = await _goalRepository.GetAllAsync();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _handler = new GetGoalHandler(_goalQueries);
    }

    [Test]
    public async Task HandleAsync_WithValidGoalId_ReturnsGoal()
    {
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 3, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .WithProgress(50m)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var query = new GetGoalQuery { GoalId = goal.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(goal.Id.Value));
            Assert.That(result.RefDate, Is.EqualTo(new DateOnly(2024, 3, 15)));
            Assert.That(result.Period, Is.EqualTo((int)GoalPeriods.Monthly));
            Assert.That(result.Progress, Is.EqualTo(50m));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentGoalId_ReturnsNull()
    {
        var query = new GetGoalQuery { GoalId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_MapsGoalTypeCorrectly()
    {
        var goal = GoalBuilder.AStackBitcoinGoal(2_000_000).Build();
        await _goalRepository.SaveAsync(goal);

        var query = new GetGoalQuery { GoalId = goal.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GoalType, Is.TypeOf<StackBitcoinGoalTypeOutputDTO>());
        var goalType = (StackBitcoinGoalTypeOutputDTO)result.GoalType;
        Assert.That(goalType.TargetSats, Is.EqualTo(2_000_000));
    }

    [Test]
    public async Task HandleAsync_MapsBitcoinHodlGoalTypeCorrectly()
    {
        var goal = GoalBuilder.ABitcoinHodlGoal(100_000).Build();
        await _goalRepository.SaveAsync(goal);

        var query = new GetGoalQuery { GoalId = goal.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GoalType, Is.TypeOf<BitcoinHodlGoalTypeOutputDTO>());
        var goalType = (BitcoinHodlGoalTypeOutputDTO)result.GoalType;
        Assert.That(goalType.MaxSellableSats, Is.EqualTo(100_000));
    }

    [Test]
    public async Task HandleAsync_MapsSpendingLimitGoalTypeCorrectly()
    {
        var goal = GoalBuilder.ASpendingLimitGoal(1500m).Build();
        await _goalRepository.SaveAsync(goal);

        var query = new GetGoalQuery { GoalId = goal.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GoalType, Is.TypeOf<SpendingLimitGoalTypeOutputDTO>());
        var goalType = (SpendingLimitGoalTypeOutputDTO)result.GoalType;
        Assert.That(goalType.TargetAmount, Is.EqualTo(1500m));
    }
}
