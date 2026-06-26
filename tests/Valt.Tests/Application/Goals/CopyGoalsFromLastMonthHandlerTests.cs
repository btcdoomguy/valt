using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.Commands.CopyGoalsFromLastMonth;
using Valt.App.Modules.Goals.Notifications;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Goals;

[TestFixture]
public class CopyGoalsFromLastMonthHandlerTests : DatabaseTest
{
    private CopyGoalsFromLastMonthHandler _handler = null!;
    private INotificationPublisher _notificationPublisher = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing goals from previous tests
        var existingGoals = await _goalRepository.GetAllAsync();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _notificationPublisher = Substitute.For<INotificationPublisher>();
        _handler = new CopyGoalsFromLastMonthHandler(_goalRepository, _notificationPublisher);
    }

    [Test]
    public async Task HandleAsync_CopiesMonthlyGoalsFromPreviousMonth()
    {
        var previousMonthGoal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 1, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .WithProgress(50m)
            .Build();
        await _goalRepository.SaveAsync(previousMonthGoal);

        var command = new CopyGoalsFromLastMonthCommand
        {
            CurrentDate = new DateOnly(2024, 2, 10)
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.CopiedCount, Is.EqualTo(1));

        var allGoals = await _goalRepository.GetAllAsync();
        var currentMonthGoals = allGoals.Where(g =>
            g.RefDate.Year == 2024 && g.RefDate.Month == 2).ToList();

        Assert.That(currentMonthGoals, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(currentMonthGoals[0].RefDate.Month, Is.EqualTo(2));
            Assert.That(currentMonthGoals[0].Progress, Is.EqualTo(0)); // Progress should be reset
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotCopyYearlyGoals()
    {
        var yearlyGoal = GoalBuilder.AStackBitcoinGoal(10_000_000)
            .WithRefDate(new DateOnly(2024, 1, 15))
            .WithPeriod(GoalPeriods.Yearly)
            .Build();
        await _goalRepository.SaveAsync(yearlyGoal);

        var command = new CopyGoalsFromLastMonthCommand
        {
            CurrentDate = new DateOnly(2024, 2, 10)
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.CopiedCount, Is.EqualTo(0));
    }

    [Test]
    public async Task HandleAsync_DoesNotDuplicateExistingGoals()
    {
        // Create a goal in January
        var januaryGoal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 1, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(januaryGoal);

        // Create the same goal type in February (already exists)
        var februaryGoal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 2, 1))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(februaryGoal);

        var command = new CopyGoalsFromLastMonthCommand
        {
            CurrentDate = new DateOnly(2024, 2, 10)
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.CopiedCount, Is.EqualTo(0));

        var allGoals = await _goalRepository.GetAllAsync();
        var currentMonthGoals = allGoals.Where(g =>
            g.RefDate.Year == 2024 && g.RefDate.Month == 2).ToList();

        Assert.That(currentMonthGoals, Has.Count.EqualTo(1)); // No duplicate created
    }

    [Test]
    public async Task HandleAsync_CopiesMultipleGoals()
    {
        var goal1 = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 1, 1))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        var goal2 = GoalBuilder.ASpendingLimitGoal(500m)
            .WithRefDate(new DateOnly(2024, 1, 1))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal1);
        await _goalRepository.SaveAsync(goal2);

        var command = new CopyGoalsFromLastMonthCommand
        {
            CurrentDate = new DateOnly(2024, 2, 15)
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.CopiedCount, Is.EqualTo(2));
    }

    [Test]
    public async Task HandleAsync_PublishesGoalProgressUpdateRequested_WhenGoalsCopied()
    {
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 1, 1))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var command = new CopyGoalsFromLastMonthCommand
        {
            CurrentDate = new DateOnly(2024, 2, 15)
        };

        await _handler.HandleAsync(command);

        await _notificationPublisher.Received(1)
            .PublishAsync(Arg.Is<GoalProgressUpdateRequested>(_ => true));
    }

    [Test]
    public async Task HandleAsync_DoesNotPublishGoalProgressUpdateRequested_WhenNoGoalsCopied()
    {
        var command = new CopyGoalsFromLastMonthCommand
        {
            CurrentDate = new DateOnly(2024, 2, 15)
        };

        await _handler.HandleAsync(command);

        await _notificationPublisher.DidNotReceive()
            .PublishAsync(Arg.Any<GoalProgressUpdateRequested>());
    }
}
