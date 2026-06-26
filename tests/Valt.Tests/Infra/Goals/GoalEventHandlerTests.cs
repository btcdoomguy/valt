using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Modules.Goals.Contracts;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Events;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Goals.Handlers;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Goals;

[TestFixture]
public class GoalEventHandlerTests
{
    private GoalEventHandler _handler = null!;
    private BackgroundJobManager _manager = null!;
    private FakeGoalProgressJob _fakeJob = null!;
    private IGoalProgressState _progressState = null!;
    private ILogger<GoalEventHandler> _logger = null!;

    private long _markTimestamp;
    private long _runTimestamp;

    [OneTimeSetUp]
    public void OneTimeSetUp() => IdGenerator.Configure(new LiteDbIdProvider());

    [SetUp]
    public void SetUp()
    {
        _markTimestamp = 0;
        _runTimestamp = 0;

        _fakeJob = new FakeGoalProgressJob(() => _runTimestamp = DateTime.UtcNow.Ticks);
        _manager = new BackgroundJobManager([_fakeJob]);
        _manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false).Wait();

        _progressState = Substitute.For<IGoalProgressState>();
        _progressState.When(x => x.MarkAsStale()).Do(_ => _markTimestamp = DateTime.UtcNow.Ticks);

        _logger = Substitute.For<ILogger<GoalEventHandler>>();
        _handler = new GoalEventHandler(_progressState, _manager);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _manager.StopAll();
        await _manager.DisposeAsync();
        (_progressState as IDisposable)?.Dispose();
        (_logger as IDisposable)?.Dispose();
    }

    [Test]
    public async Task HandleAsync_GoalCreatedEvent_MarksStaleThenTriggersJob()
    {
        var goal = GoalBuilder.AStackBitcoinGoal().Build();

        await _handler.HandleAsync(new GoalCreatedEvent(goal));
        await _manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.GoalProgressUpdater);

        _progressState.Received(1).MarkAsStale();
        Assert.That(_fakeJob.RunCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(_runTimestamp, Is.GreaterThan(_markTimestamp));
    }

    [Test]
    public async Task HandleAsync_GoalUpdatedEvent_MarksStaleThenTriggersJob()
    {
        var goal = GoalBuilder.AStackBitcoinGoal().Build();

        await _handler.HandleAsync(new GoalUpdatedEvent(goal));
        await _manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.GoalProgressUpdater);

        _progressState.Received(1).MarkAsStale();
        Assert.That(_fakeJob.RunCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(_runTimestamp, Is.GreaterThan(_markTimestamp));
    }

    [Test]
    public async Task HandleAsync_GoalDeletedEvent_MarksStaleThenTriggersJob()
    {
        var goal = GoalBuilder.AStackBitcoinGoal().Build();

        await _handler.HandleAsync(new GoalDeletedEvent(goal));
        await _manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.GoalProgressUpdater);

        _progressState.Received(1).MarkAsStale();
        Assert.That(_fakeJob.RunCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(_runTimestamp, Is.GreaterThan(_markTimestamp));
    }

    private class FakeGoalProgressJob : IBackgroundJob
    {
        private readonly Action? _onRun;
        private int _runCount;

        public string Name => "Fake Goal Progress Updater";
        public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.GoalProgressUpdater;
        public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
        public TimeSpan Interval => TimeSpan.FromMinutes(5);
        public int RunCount => _runCount;

        public FakeGoalProgressJob(Action? onRun = null)
        {
            _onRun = onRun;
        }

        public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public Task RunAsync(CancellationToken stoppingToken)
        {
            Interlocked.Increment(ref _runCount);
            _onRun?.Invoke();
            return Task.CompletedTask;
        }
    }
}
