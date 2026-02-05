namespace Valt.Infra.Kernel.BackgroundJobs;

public interface IBackgroundJob
{
    string Name { get; }
    BackgroundJobSystemNames SystemName { get; }
    BackgroundJobTypes JobType { get; }
    TimeSpan Interval { get; }
    Task StartAsync(CancellationToken stoppingToken);
    Task RunAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Resets any internal state that should be cleared when the job is stopped.
    /// Default implementation does nothing. Override to clear cached state.
    /// </summary>
    void ResetState() { }
}