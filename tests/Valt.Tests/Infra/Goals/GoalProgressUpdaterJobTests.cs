using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Goals;

[TestFixture]
public class GoalProgressUpdaterJobTests
{
    private GoalProgressUpdaterJob _job = null!;
    private ILocalDatabase _localDatabase = null!;
    private IGoalQueries _goalQueries = null!;
    private IGoalRepository _goalRepository = null!;
    private IGoalProgressCalculatorFactory _calculatorFactory = null!;
    private GoalProgressState _progressState = null!;
    private INotificationPublisher _notificationPublisher = null!;
    private IClock _clock = null!;
    private ILogger<GoalProgressUpdaterJob> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _localDatabase = Substitute.For<ILocalDatabase>();
        _goalQueries = Substitute.For<IGoalQueries>();
        _goalRepository = Substitute.For<IGoalRepository>();
        _calculatorFactory = Substitute.For<IGoalProgressCalculatorFactory>();
        _progressState = new GoalProgressState();
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        _clock = new FakeClock(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc));
        _logger = Substitute.For<ILogger<GoalProgressUpdaterJob>>();

        _localDatabase.HasDatabaseOpen.Returns(true);
        _goalQueries.GetStaleGoalsAsync().Returns(Array.Empty<StaleGoalDTO>());

        _job = new GoalProgressUpdaterJob(
            _localDatabase,
            _goalQueries,
            _goalRepository,
            _calculatorFactory,
            _progressState,
            _notificationPublisher,
            _clock,
            _logger);
    }

    [TearDown]
    public void TearDown()
    {
        (_localDatabase as IDisposable)?.Dispose();
        (_goalQueries as IDisposable)?.Dispose();
        (_goalRepository as IDisposable)?.Dispose();
        (_calculatorFactory as IDisposable)?.Dispose();
        (_notificationPublisher as IDisposable)?.Dispose();
        (_logger as IDisposable)?.Dispose();
    }

    [Test]
    public void Interval_Should_Be_At_Least_120_Seconds()
    {
        Assert.That(_job.Interval, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(60)));
        Assert.That(_job.Interval, Is.EqualTo(TimeSpan.FromSeconds(120)));
    }

    [Test]
    public async Task RunAsync_When_Database_Not_Open_Should_Not_Query()
    {
        _localDatabase.HasDatabaseOpen.Returns(false);

        await _job.RunAsync(CancellationToken.None);

        await _goalQueries.DidNotReceive().GetStaleGoalsAsync();
    }

    [Test]
    public async Task RunAsync_When_Bootstrap_Not_Completed_Should_Query_And_Mark_Bootstrap_Completed()
    {
        Assert.That(_progressState.BootstrapCompleted, Is.False);

        await _job.RunAsync(CancellationToken.None);

        Assert.That(_progressState.BootstrapCompleted, Is.True);
        await _goalQueries.Received(1).GetStaleGoalsAsync();
    }

    [Test]
    public async Task RunAsync_When_No_Stale_Goals_Should_Not_Query()
    {
        _progressState.MarkBootstrapCompleted();
        Assert.That(_progressState.HasStaleGoals, Is.False);

        await _job.RunAsync(CancellationToken.None);

        await _goalQueries.DidNotReceive().GetStaleGoalsAsync();
    }

    [Test]
    public async Task RunAsync_When_Stale_Flag_Set_Should_Query_And_Clear_Flag()
    {
        _progressState.MarkBootstrapCompleted();
        _progressState.MarkAsStale();

        await _job.RunAsync(CancellationToken.None);

        Assert.That(_progressState.HasStaleGoals, Is.False);
        await _goalQueries.Received(1).GetStaleGoalsAsync();
    }
}
