using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Tests.Infrastructure.BackgroundJobs;

[TestFixture]
public class BackgroundJobManagerTests
{
    #region Test Job Implementations

    private class TestJob : IBackgroundJob
    {
        private readonly Func<CancellationToken, Task>? _onRun;
        private int _runCount;

        public string Name => "Test Job";
        public BackgroundJobSystemNames SystemName { get; init; } = BackgroundJobSystemNames.Foo;
        public BackgroundJobTypes JobType { get; init; } = BackgroundJobTypes.ValtDatabase;
        public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(5);
        public int RunCount => _runCount;

        public TestJob(Func<CancellationToken, Task>? onRun = null)
        {
            _onRun = onRun;
        }

        public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            Interlocked.Increment(ref _runCount);
            if (_onRun != null)
                await _onRun(stoppingToken);
        }
    }

    private class FailingJob : IBackgroundJob
    {
        private readonly int _failCount;
        private int _attemptCount;

        public string Name => "Failing Job";
        public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.Foo;
        public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
        public TimeSpan Interval => TimeSpan.FromMinutes(5);
        public int AttemptCount => _attemptCount;

        public FailingJob(int failCount = int.MaxValue)
        {
            _failCount = failCount;
        }

        public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public Task RunAsync(CancellationToken stoppingToken)
        {
            Interlocked.Increment(ref _attemptCount);
            if (_attemptCount <= _failCount)
                throw new InvalidOperationException($"Attempt {_attemptCount} failed");
            return Task.CompletedTask;
        }
    }

    #endregion

    #region Job Lifecycle Tests

    [Test]
    public async Task StartAllJobs_Should_Start_Jobs_Of_Specified_Type()
    {
        // Arrange
        var job = new TestJob();
        await using var manager = new BackgroundJobManager([job]);

        // Act
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Assert: Job should exist in manager
        var jobInfo = manager.GetJobInfos().First();
        Assert.That(jobInfo.Job.Name, Is.EqualTo("Test Job"));
    }

    [Test]
    public async Task StopAll_Should_Stop_All_Running_Jobs()
    {
        // Arrange
        var job = new TestJob();
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        await manager.StopAll();

        // Assert
        var jobInfo = manager.GetJobInfos().First();
        Assert.That(jobInfo.State, Is.EqualTo(BackgroundJobState.Stopped));
    }

    #endregion

    #region Manual Trigger Tests

    [Test]
    public async Task TriggerJobAndWaitAsync_Should_Execute_Job()
    {
        // Arrange
        var job = new TestJob();
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo);

        // Assert
        Assert.That(job.RunCount, Is.EqualTo(1));
    }

    [Test]
    public async Task TriggerJobAndWaitAsync_Should_Throw_When_Job_Fails()
    {
        // Arrange
        var job = new FailingJob(failCount: 10); // Will always fail
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo));
    }

    #endregion

    #region Retry Logic Tests

    [Test]
    public async Task Should_Retry_Failed_Job_Up_To_3_Times()
    {
        // Arrange
        var job = new FailingJob(failCount: 10); // Will always fail
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        try
        {
            await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo);
        }
        catch
        {
            // Expected
        }

        // Assert: Should have attempted 3 times (MaxRetryAttempts = 3)
        Assert.That(job.AttemptCount, Is.EqualTo(3));
    }

    [Test]
    public async Task Should_Succeed_On_Retry_If_Job_Eventually_Succeeds()
    {
        // Arrange: Job fails twice then succeeds
        var job = new FailingJob(failCount: 2);
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo);

        // Assert: Should have attempted 3 times (2 failures + 1 success)
        Assert.That(job.AttemptCount, Is.EqualTo(3));
    }

    #endregion

    #region State Management Tests

    [Test]
    public async Task State_Should_Be_Ok_After_Successful_Execution()
    {
        // Arrange
        var job = new TestJob();
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo);

        // Assert
        var jobInfo = manager.GetJobInfos().First();
        Assert.That(jobInfo.State, Is.EqualTo(BackgroundJobState.Ok));
    }

    [Test]
    public async Task State_Should_Be_Error_After_Failed_Execution()
    {
        // Arrange
        var job = new FailingJob();
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        try
        {
            await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo);
        }
        catch
        {
            // Expected
        }

        // Assert
        var jobInfo = manager.GetJobInfos().First();
        Assert.That(jobInfo.State, Is.EqualTo(BackgroundJobState.Error));
    }

    [Test]
    public async Task HasErrors_Should_Return_True_When_Job_Has_Error()
    {
        // Arrange
        var job = new FailingJob();
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        try
        {
            await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo);
        }
        catch
        {
            // Expected
        }

        // Assert
        Assert.That(manager.HasErrors(BackgroundJobTypes.ValtDatabase), Is.True);
    }

    [Test]
    public async Task HasErrors_Should_Return_False_When_No_Errors()
    {
        // Arrange
        var job = new TestJob();
        await using var manager = new BackgroundJobManager([job]);
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);

        // Act
        await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.Foo);

        // Assert
        Assert.That(manager.HasErrors(BackgroundJobTypes.ValtDatabase), Is.False);
    }

    #endregion

    #region Multiple Job Types Tests

    [Test]
    public async Task Should_Only_Start_Jobs_Of_Specified_Type()
    {
        // Arrange - each job needs a unique SystemName
        var valtJob = new TestJob { JobType = BackgroundJobTypes.ValtDatabase, SystemName = BackgroundJobSystemNames.AutoSatAmountUpdater };
        var priceJob = new TestJob { JobType = BackgroundJobTypes.PriceDatabase, SystemName = BackgroundJobSystemNames.LivePricesUpdater };
        await using var manager = new BackgroundJobManager([valtJob, priceJob]);

        // Act: Start only ValtDatabase jobs and trigger them
        await manager.StartAllJobsAsync(BackgroundJobTypes.ValtDatabase, triggerInitialRun: false);
        await manager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.AutoSatAmountUpdater);

        // Assert: Only ValtDatabase job should have run
        Assert.That(valtJob.RunCount, Is.EqualTo(1));
        Assert.That(priceJob.RunCount, Is.EqualTo(0));
    }

    #endregion
}
