using NSubstitute;
using Valt.App.Modules.Goals.Commands.RecalculateGoal;
using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Services;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Goals;

[TestFixture]
public class RecalculateGoalHandlerTests : DatabaseTest
{
    private RecalculateGoalHandler _handler = null!;
    private GoalProgressState _goalProgressState = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing goals from previous tests
        var existingGoals = await _goalRepository.GetAllAsync();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _goalProgressState = new GoalProgressState();
        _handler = new RecalculateGoalHandler(_goalRepository, _goalProgressState);
    }

    [Test]
    public async Task HandleAsync_WithCompletedGoal_ResetsStateToOpen()
    {
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithState(GoalStates.Completed)
            .WithProgress(100m)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var command = new RecalculateGoalCommand { GoalId = goal.Id.Value };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedGoal = await _goalRepository.GetByIdAsync(goal.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updatedGoal!.State, Is.EqualTo(GoalStates.Open));
            Assert.That(updatedGoal.IsUpToDate, Is.False);
        });
    }

    [Test]
    public async Task HandleAsync_WithFailedGoal_ResetsStateToOpen()
    {
        var goal = GoalBuilder.ABitcoinHodlGoal(0)
            .WithState(GoalStates.Failed)
            .WithProgress(100m)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var command = new RecalculateGoalCommand { GoalId = goal.Id.Value };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedGoal = await _goalRepository.GetByIdAsync(goal.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updatedGoal!.State, Is.EqualTo(GoalStates.Open));
            Assert.That(updatedGoal.IsUpToDate, Is.False);
        });
    }

    [Test]
    public async Task HandleAsync_WithOpenGoal_ReturnsError()
    {
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithState(GoalStates.Open)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var command = new RecalculateGoalCommand { GoalId = goal.Id.Value };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_STATE"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyGoalId_ReturnsValidationError()
    {
        var command = new RecalculateGoalCommand { GoalId = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentGoalId_ReturnsNotFound()
    {
        var command = new RecalculateGoalCommand { GoalId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GOAL_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_MarksProgressStateAsStale()
    {
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithState(GoalStates.Completed)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var command = new RecalculateGoalCommand { GoalId = goal.Id.Value };

        await _handler.HandleAsync(command);

        Assert.That(_goalProgressState.HasStaleGoals, Is.True);
    }
}
