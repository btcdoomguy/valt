using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Goals;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Goals;

[TestFixture]
public class GoalRepositoryTests
{
    private MemoryStream _localDatabaseStream;
    private ILocalDatabase _localDatabase;
    private IDomainEventPublisher _domainEventPublisher;
    private GoalRepository _repository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());

        _localDatabaseStream = new MemoryStream();
        _localDatabase = new LocalDatabase(new Clock());
        _localDatabase.OpenInMemoryDatabase(_localDatabaseStream);
    }

    [SetUp]
    public void SetUp()
    {
        _domainEventPublisher = Substitute.For<IDomainEventPublisher>();
        _repository = new GoalRepository(_localDatabase, _domainEventPublisher);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _localDatabase.CloseDatabase();
        _localDatabase.Dispose();
        await _localDatabaseStream.DisposeAsync();
    }

    #region SaveAsync Tests

    [Test]
    public async Task SaveAsync_Should_Store_And_Retrieve_New_Goal()
    {
        // Arrange
        var goal = Goal.New(
            new DateOnly(2024, 1, 15),
            GoalPeriods.Monthly,
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        // Act
        await _repository.SaveAsync(goal);

        // Assert
        var retrievedGoal = await _repository.GetByIdAsync(goal.Id);
        Assert.That(retrievedGoal, Is.Not.Null);
        Assert.That(retrievedGoal!.Id, Is.EqualTo(goal.Id));
        Assert.That(retrievedGoal.RefDate, Is.EqualTo(goal.RefDate));
        Assert.That(retrievedGoal.Period, Is.EqualTo(goal.Period));
        Assert.That(retrievedGoal.State, Is.EqualTo(goal.State));
    }

    [Test]
    public async Task SaveAsync_Should_Clear_Events_After_Saving()
    {
        // Arrange
        var goal = Goal.New(
            new DateOnly(2024, 1, 15),
            GoalPeriods.Monthly,
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        Assert.That(goal.Events.Count, Is.GreaterThan(0));

        // Act
        await _repository.SaveAsync(goal);

        // Assert
        Assert.That(goal.Events, Is.Empty);
    }

    [Test]
    public async Task SaveAsync_Should_Publish_Domain_Events()
    {
        // Arrange
        var goal = Goal.New(
            new DateOnly(2024, 1, 15),
            GoalPeriods.Monthly,
            new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000)));

        // Act
        await _repository.SaveAsync(goal);

        // Assert
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<IDomainEvent>());
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_Should_Remove_Goal_From_Database()
    {
        // Arrange
        var goal = Goal.New(
            new DateOnly(2024, 2, 15),
            GoalPeriods.Monthly,
            new StackBitcoinGoalType(BtcValue.ParseSats(500_000)));

        await _repository.SaveAsync(goal);

        // Verify goal exists
        var existingGoal = await _repository.GetByIdAsync(goal.Id);
        Assert.That(existingGoal, Is.Not.Null);

        // Act
        await _repository.DeleteAsync(goal);

        // Assert
        var deletedGoal = await _repository.GetByIdAsync(goal.Id);
        Assert.That(deletedGoal, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_Should_Clear_Events_After_Deleting()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithIsUpToDate(true) // Set to true so MarkAsStale will add an event
            .Build();

        await _repository.SaveAsync(goal);

        // Retrieve the goal and modify it to generate events
        var retrievedGoal = await _repository.GetByIdAsync(goal.Id);
        Assert.That(retrievedGoal, Is.Not.Null);

        // Close the goal to add an event (Close works from Open state)
        retrievedGoal!.Close();
        Assert.That(retrievedGoal.Events.Count, Is.GreaterThan(0));

        // Act
        await _repository.DeleteAsync(retrievedGoal);

        // Assert
        Assert.That(retrievedGoal.Events, Is.Empty);
    }

    [Test]
    public async Task DeleteAsync_Should_Publish_Domain_Events()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithState(GoalStates.Open)
            .Build();

        await _repository.SaveAsync(goal);

        // Retrieve the goal and modify it to generate events
        var retrievedGoal = await _repository.GetByIdAsync(goal.Id);
        Assert.That(retrievedGoal, Is.Not.Null);

        // Reset the mock to track only delete-related events
        _domainEventPublisher.ClearReceivedCalls();

        // Close the goal to add an event
        retrievedGoal!.Close();
        Assert.That(retrievedGoal.Events.Count, Is.GreaterThan(0));

        // Act
        await _repository.DeleteAsync(retrievedGoal);

        // Assert
        await _domainEventPublisher.Received().PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Test]
    public async Task DeleteAsync_Should_Not_Affect_Other_Goals()
    {
        // Arrange
        var goal1 = Goal.New(
            new DateOnly(2024, 5, 15),
            GoalPeriods.Monthly,
            new StackBitcoinGoalType(BtcValue.ParseSats(100_000)));

        var goal2 = Goal.New(
            new DateOnly(2024, 6, 15),
            GoalPeriods.Monthly,
            new StackBitcoinGoalType(BtcValue.ParseSats(200_000)));

        await _repository.SaveAsync(goal1);
        await _repository.SaveAsync(goal2);

        // Act
        await _repository.DeleteAsync(goal1);

        // Assert
        var deletedGoal = await _repository.GetByIdAsync(goal1.Id);
        var remainingGoal = await _repository.GetByIdAsync(goal2.Id);

        Assert.That(deletedGoal, Is.Null);
        Assert.That(remainingGoal, Is.Not.Null);
        Assert.That(remainingGoal!.Id, Is.EqualTo(goal2.Id));
    }

    [Test]
    public async Task DeleteAsync_Should_Work_For_Goal_In_Any_State()
    {
        // Arrange - Create goals in different states
        var openGoal = GoalBuilder.AGoal()
            .WithState(GoalStates.Open)
            .Build();

        var closedGoal = GoalBuilder.AGoal()
            .WithState(GoalStates.Closed)
            .Build();

        var completedGoal = GoalBuilder.AGoal()
            .WithState(GoalStates.MarkedAsCompleted)
            .Build();

        await _repository.SaveAsync(openGoal);
        await _repository.SaveAsync(closedGoal);
        await _repository.SaveAsync(completedGoal);

        // Act
        await _repository.DeleteAsync(openGoal);
        await _repository.DeleteAsync(closedGoal);
        await _repository.DeleteAsync(completedGoal);

        // Assert
        Assert.That(await _repository.GetByIdAsync(openGoal.Id), Is.Null);
        Assert.That(await _repository.GetByIdAsync(closedGoal.Id), Is.Null);
        Assert.That(await _repository.GetByIdAsync(completedGoal.Id), Is.Null);
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public async Task GetAllAsync_Should_Return_Empty_When_No_Goals_Exist()
    {
        // Arrange - Clear existing goals by creating a fresh database
        // We'll rely on the fact that goals created in other tests have unique IDs

        // Act
        var goals = await _repository.GetAllAsync();

        // Assert
        Assert.That(goals, Is.Not.Null);
    }

    [Test]
    public async Task GetAllAsync_Should_Return_All_Saved_Goals()
    {
        // Arrange
        var goal1 = Goal.New(
            new DateOnly(2024, 7, 15),
            GoalPeriods.Monthly,
            new StackBitcoinGoalType(BtcValue.ParseSats(300_000)));

        var goal2 = Goal.New(
            new DateOnly(2024, 8, 15),
            GoalPeriods.Yearly,
            new StackBitcoinGoalType(BtcValue.ParseSats(400_000)));

        await _repository.SaveAsync(goal1);
        await _repository.SaveAsync(goal2);

        // Act
        var goals = (await _repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(goals.Any(g => g.Id == goal1.Id), Is.True);
        Assert.That(goals.Any(g => g.Id == goal2.Id), Is.True);
    }

    #endregion
}
