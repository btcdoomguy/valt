using Microsoft.Extensions.Logging;

namespace Valt.Infra.Kernel.BackgroundJobs;

public sealed class JobLoggerProvider : ILoggerProvider
{
    private readonly BackgroundJobManager _jobManager;

    public JobLoggerProvider(BackgroundJobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new JobLogger(categoryName, _jobManager);
    }

    public void Dispose()
    {
    }
}

internal sealed class JobLogger : ILogger
{
    private readonly string _categoryName;
    private readonly BackgroundJobManager _jobManager;

    public JobLogger(string categoryName, BackgroundJobManager jobManager)
    {
        _categoryName = categoryName;
        _jobManager = jobManager;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        // Find the job by category name (which typically includes the job class name)
        var jobInfo = FindJobByCategoryName();

        if (jobInfo != null)
        {
            var level = ConvertLogLevel(logLevel);
            jobInfo.LogPool.AddEntry(level, message);

            if (exception != null)
            {
                jobInfo.LogPool.AddEntry(JobLogLevel.Error, $"Exception: {exception.GetType().Name}: {exception.Message}");
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    jobInfo.LogPool.AddEntry(JobLogLevel.Error, $"StackTrace: {exception.StackTrace}");
                }
            }
        }
    }

    private JobInfo? FindJobByCategoryName()
    {
        // Category name is typically the full type name of the class requesting the logger
        // e.g., "Valt.Infra.Crawlers.LivePriceCrawlers.LivePricesUpdaterJob"
        foreach (var jobInfo in _jobManager.GetJobInfos())
        {
            var jobTypeName = jobInfo.Job.GetType().FullName;
            if (jobTypeName != null && _categoryName.Contains(jobTypeName))
            {
                return jobInfo;
            }

            // Also check if category name ends with job type name
            var shortTypeName = jobInfo.Job.GetType().Name;
            if (_categoryName.EndsWith(shortTypeName))
            {
                return jobInfo;
            }
        }

        return null;
    }

    private static JobLogLevel ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => JobLogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Debug => JobLogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => JobLogLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Warning => JobLogLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error => JobLogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => JobLogLevel.Error,
            _ => JobLogLevel.Info
        };
    }
}
