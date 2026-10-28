using System.Reflection;
using LiteDB;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Goals.Handlers;
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

/// <summary>
/// End-to-end recompute-chain proof for the GAP-1 closure criterion: a SetExcluded toggle on a
/// transaction inside a goal period, saved through the repository, flows through
/// TransactionEditedEvent → MarkGoalsStaleEventHandler → GoalProgressUpdaterJob and recomputes
/// progress on a fresh in-memory DB with fresh state objects — no app-restart equivalent.
/// The period mixes a legacy document (no excl field) with modern documents so the
/// recomputation proves both halves of the fix: flagged docs drop, legacy docs keep counting.
/// </summary>
[TestFixture]
public class GoalExclusionRecomputeChainTests : DatabaseTest
{
    private BackgroundJobManager _manager = null!;
    private GoalProgressState _progressState = null!;
    private MarkGoalsStaleEventHandler _handler = null!;

    private GoalId _goalId = null!;
    private TransactionId _modernTxId = null!;

    [SetUp]
    public new async Task SetUp()
    {
        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetGoals().DeleteAll();

        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 1, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        _goalId = goal.Id;
        await _goalRepository.SaveAsync(goal);

        _progressState = new GoalProgressState();
        var calculatorFactory = new GoalProgressCalculatorFactory(
            new IGoalProgressCalculator[] { new StackBitcoinProgressCalculator(_localDatabase) });
        var job = new GoalProgressUpdaterJob(
            _localDatabase,
            _goalQueries,
            _goalRepository,
            calculatorFactory,
            _progressState,
            Substitute.For<INotificationPublisher>(),
            new FakeClock(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc)),
            Substitute.For<ILogger<GoalProgressUpdaterJob>>());

        _manager = new BackgroundJobManager([job]);
        await _manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        _handler = new MarkGoalsStaleEventHandler(_goalRepository, _progressState, _manager);
    }

    [TearDown]
    public async Task TearDownChain()
    {
        await _manager.StopAll();
        await _manager.DisposeAsync();
    }

    [Test]
    public async Task Excluded_Flag_Toggle_Recomputes_Progress_Keeping_Legacy_Docs_Contributing()
    {
        // Arrange: legacy purchase (no excl field) + modern purchase inside the goal period
        InsertLegacyPurchase(new DateOnly(2024, 1, 5), 100_000);

        var modern = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 10))
            .WithName("Modern Purchase")
            .AsBitcoinPurchase(satAmount: 200_000, fiatAmount: 1000m)
            .BuildDomainObject();
        await _transactionRepository.SaveTransactionAsync(modern);
        _modernTxId = modern.Id;

        // Bootstrap recompute on a fresh state object — the production cold-start equivalent
        await _manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.GoalProgressUpdater);
        var goal = await _goalRepository.GetByIdAsync(_goalId);
        Assert.That(goal!.Progress, Is.EqualTo(30m));   // (100k legacy + 200k modern) / 1M

        // Act: flag-only edit on the modern transaction, saved through the repository
        var reloaded = await _transactionRepository.GetTransactionByIdAsync(_modernTxId);
        reloaded!.SetExcluded(true);

        var emitted = reloaded.Events.Single();
        Assert.That(emitted, Is.TypeOf<TransactionEditedEvent>());   // (1) toggle emits the event

        await _transactionRepository.SaveTransactionAsync(reloaded);

        // (2) + (3): handler marks the covering goal stale and triggers the job, which
        // recomputes progress — live wiring end to end, no restart
        await _handler.HandleAsync((TransactionEditedEvent)emitted);
        await _manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.GoalProgressUpdater);

        // Assert: recomputed progress keeps the legacy doc and drops only the flagged one
        var updated = await _goalRepository.GetByIdAsync(_goalId);
        Assert.That(updated!.Progress, Is.EqualTo(10m));
    }

    /// <summary>
    /// Inserts a purchase document with the "excl" field stripped, simulating a document
    /// written before the exclusion flag existed (pre-phase-53 legacy shape).
    /// </summary>
    private void InsertLegacyPurchase(DateOnly date, long satAmount)
    {
        var entity = TransactionBuilder.ATransaction()
            .WithDate(date)
            .WithName("Legacy Purchase")
            .AsBitcoinPurchase(satAmount: satAmount, fiatAmount: 100m)
            .Build();

        var database = GetUnderlyingDatabase();
        var document = database.Mapper.ToDocument(entity);
        document.Remove("excl");
        database.GetCollection("budget_transactions").Insert(document);
    }

    private LiteDatabase GetUnderlyingDatabase()
    {
        var field = typeof(LocalDatabase).GetField("_database",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (LiteDatabase)field!.GetValue(_localDatabase)!;
    }
}
