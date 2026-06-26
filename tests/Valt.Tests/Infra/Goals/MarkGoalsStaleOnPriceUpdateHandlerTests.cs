using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.Contracts;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Goals.Handlers;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Goals;

[TestFixture]
public class MarkGoalsStaleOnPriceUpdateHandlerTests
{
    private MarkGoalsStaleOnPriceUpdateHandler _handler = null!;
    private BackgroundJobManager _manager = null!;
    private FakeGoalProgressJob _fakeJob = null!;
    private IGoalRepository _goalRepository = null!;
    private IGoalProgressState _progressState = null!;
    private ILogger<MarkGoalsStaleOnPriceUpdateHandler> _logger = null!;

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

        _goalRepository = Substitute.For<IGoalRepository>();
        _goalRepository.SaveAsync(Arg.Any<Goal>()).Returns(Task.CompletedTask);

        _progressState = Substitute.For<IGoalProgressState>();
        _progressState.When(x => x.MarkAsStale()).Do(_ => _markTimestamp = DateTime.UtcNow.Ticks);

        _logger = Substitute.For<ILogger<MarkGoalsStaleOnPriceUpdateHandler>>();
        _handler = new MarkGoalsStaleOnPriceUpdateHandler(_goalRepository, _progressState, _manager, _logger);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _manager.StopAll();
        await _manager.DisposeAsync();
        (_goalRepository as IDisposable)?.Dispose();
        (_progressState as IDisposable)?.Dispose();
        (_logger as IDisposable)?.Dispose();
    }

    [Test]
    public async Task HandleAsync_WhenNoPriceDependentGoals_DoesNotMarkStaleOrTriggerJob()
    {
        var goal = GoalBuilder.AStackBitcoinGoal()
            .WithIsUpToDate(true)
            .Build();
        _goalRepository.GetAllAsync().Returns(new[] { goal });

        await _handler.HandleAsync(new FiatHistoryPriceUpdatedMessage());
        await Task.Delay(100); // Give any queued run a chance to process

        _progressState.DidNotReceive().MarkAsStale();
        Assert.That(_fakeJob.RunCount, Is.EqualTo(0));
    }

    [Test]
    public async Task HandleAsync_WhenPriceDependentGoalsExist_MarksStaleThenTriggersJob()
    {
        var goal = GoalBuilder.ASaveFiatGoal()
            .WithIsUpToDate(true)
            .Build();
        _goalRepository.GetAllAsync().Returns(new[] { goal });

        await _handler.HandleAsync(new BitcoinHistoryPriceUpdatedMessage());
        await _manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.GoalProgressUpdater);

        await _goalRepository.Received(1).SaveAsync(goal);
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
