namespace Valt.Infra.Kernel.BackgroundJobs;

public interface IBackgroundJob
{
    string Name { get; }
    BackgroundJobSystemNames SystemName { get; }
    BackgroundJobTypes JobType { get; }
    TimeSpan Interval { get; }
    Task StartAsync(CancellationToken stoppingToken);
    Task RunAsync(CancellationToken stoppingToken);
}