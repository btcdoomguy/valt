using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Goals;
using Valt.Infra.Kernel;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Goals;

[TestFixture]
public class GoalTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    #region UpdateProgress Tests

    [Test]
    public void Should_Update_Progress_And_Mark_As_UpToDate()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithProgress(0m)
            .WithIsUpToDate(false)
            .Build();

        // Act
        goal.UpdateProgress(50m, DateTime.UtcNow);

        // Assert
        Assert.That(goal.Progress, Is.EqualTo(50m));
        Assert.That(goal.IsUpToDate, Is.True);
    }

    [Test]
    public void Should_Update_LastUpdatedAt_When_Progress_Updated()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithLastUpdatedAt(DateTime.MinValue)
            .Build();

        var updateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        goal.UpdateProgress(75m, updateTime);

        // Assert
        Assert.That(goal.LastUpdatedAt, Is.EqualTo(updateTime));
    }

    [Test]
    public void Should_Not_Auto_Complete_When_Progress_Reaches_100()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithState(GoalStates.Open)
            .Build();

        // Act
        goal.UpdateProgress(100m, DateTime.UtcNow);

        // Assert - Manual completion, state should remain Open
        Assert.That(goal.State, Is.EqualTo(GoalStates.Open));
    }

    #endregion

    #region MarkAsStale Tests

    [Test]
    public void Should_Mark_Goal_As_Stale()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithIsUpToDate(true)
            .Build();

        // Act
        goal.MarkAsStale();

        // Assert
        Assert.That(goal.IsUpToDate, Is.False);
    }

    [Test]
    public void Should_Not_Mark_Goal_As_Stale_If_Already_Stale()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithIsUpToDate(false)
            .Build();

        var eventCountBefore = goal.Events.Count;

        // Act
        goal.MarkAsStale();

        // Assert - No new event should be added
        Assert.That(goal.Events.Count, Is.EqualTo(eventCountBefore));
    }

    #endregion

    #region GetPeriodRange Tests

    [Test]
    public void Should_Return_Monthly_Range()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithRefDate(new DateOnly(2024, 3, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();

        // Act
        var range = goal.GetPeriodRange();

        // Assert
        Assert.That(range.Start, Is.EqualTo(new DateOnly(2024, 3, 1)));
        Assert.That(range.End, Is.EqualTo(new DateOnly(2024, 3, 31)));
    }

    [Test]
    public void Should_Return_Monthly_Range_For_February_Leap_Year()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithRefDate(new DateOnly(2024, 2, 10)) // 2024 is a leap year
            .WithPeriod(GoalPeriods.Monthly)
            .Build();

        // Act
        var range = goal.GetPeriodRange();

        // Assert
        Assert.That(range.Start, Is.EqualTo(new DateOnly(2024, 2, 1)));
        Assert.That(range.End, Is.EqualTo(new DateOnly(2024, 2, 29))); // Leap year
    }

    [Test]
    public void Should_Return_Yearly_Range()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithRefDate(new DateOnly(2024, 6, 20))
            .WithPeriod(GoalPeriods.Yearly)
            .Build();

        // Act
        var range = goal.GetPeriodRange();

        // Assert
        Assert.That(range.Start, Is.EqualTo(new DateOnly(2024, 1, 1)));
        Assert.That(range.End, Is.EqualTo(new DateOnly(2024, 12, 31)));
    }

    #endregion

    #region MarkAsCompleted Tests

    [Test]
    public void Should_Mark_Goal_As_Completed()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithState(GoalStates.Open)
            .Build();

        // Act
        goal.MarkAsCompleted();

        // Assert
        Assert.That(goal.State, Is.EqualTo(GoalStates.MarkedAsCompleted));
    }

    [Test]
    public void Should_Not_Change_State_When_Already_Completed()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithState(GoalStates.Completed)
            .Build();

        // Act
        goal.MarkAsCompleted();

        // Assert - State unchanged
        Assert.That(goal.State, Is.EqualTo(GoalStates.Completed));
    }

    [Test]
    public void Should_Not_Change_State_When_MarkedAsCompleted()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithState(GoalStates.MarkedAsCompleted)
            .Build();

        // Act
        goal.MarkAsCompleted();

        // Assert - State unchanged
        Assert.That(goal.State, Is.EqualTo(GoalStates.MarkedAsCompleted));
    }

    [Test]
    public void Should_Not_Change_State_When_Closed()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithState(GoalStates.Closed)
            .Build();

        // Act
        goal.MarkAsCompleted();

        // Assert - State unchanged
        Assert.That(goal.State, Is.EqualTo(GoalStates.Closed));
    }

    #endregion

    #region Factory Methods Tests

    [Test]
    public void Should_Create_New_Goal_With_Correct_Initial_State()
    {
        // Arrange
        var refDate = new DateOnly(2024, 1, 15);
        var goalType = new Valt.Core.Modules.Goals.GoalTypes.StackBitcoinGoalType(
            Valt.Core.Common.BtcValue.ParseSats(1_000_000));

        // Act
        var goal = Goal.New(refDate, GoalPeriods.Monthly, goalType);

        // Assert
        Assert.That(goal.RefDate, Is.EqualTo(refDate));
        Assert.That(goal.Period, Is.EqualTo(GoalPeriods.Monthly));
        Assert.That(goal.GoalType, Is.EqualTo(goalType));
        Assert.That(goal.Progress, Is.EqualTo(0m));
        Assert.That(goal.IsUpToDate, Is.False);
        Assert.That(goal.State, Is.EqualTo(GoalStates.Open));
        Assert.That(goal.Events.Count, Is.EqualTo(1)); // GoalCreatedEvent
    }

    #endregion
}
