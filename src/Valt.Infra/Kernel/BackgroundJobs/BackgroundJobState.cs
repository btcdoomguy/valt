namespace Valt.Infra.Kernel.BackgroundJobs;

public enum BackgroundJobState
{
    Ok = 0,
    Error = 1,
    Stopped = 2,
    Running = 3
}