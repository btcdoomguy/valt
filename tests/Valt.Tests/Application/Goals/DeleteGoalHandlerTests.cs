using LiteDB;
using Valt.App.Modules.Goals.Commands.DeleteGoal;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Goals;

[TestFixture]
public class DeleteGoalHandlerTests : DatabaseTest
{
    private DeleteGoalHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new DeleteGoalHandler(_goalRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidGoalId_DeletesGoal()
    {
        // Create a goal first
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000).Build();
        await _goalRepository.SaveAsync(goal);

        var command = new DeleteGoalCommand
        {
            GoalId = goal.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        // Verify deletion
        var entity = _localDatabase.GetGoals().FindById(new ObjectId(goal.Id.Value));
        Assert.That(entity, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyGoalId_ReturnsValidationError()
    {
        var command = new DeleteGoalCommand
        {
            GoalId = ""
        };

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
        var command = new DeleteGoalCommand
        {
            GoalId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GOAL_NOT_FOUND"));
        });
    }
}
