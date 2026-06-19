using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Valt.UI.Base;

/// <summary>
/// Extension methods for safe fire-and-forget task handling.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Starts the task without awaiting it and routes the outcome through the supplied
    /// fire-and-forget runner. The runner logs cancellation at Debug level and failures at Error level.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="runner">The runner responsible for observing the task.</param>
    /// <param name="logger">Logger used by the runner to record cancellation or failure.</param>
    /// <param name="callerName">Name of the calling method, captured automatically.</param>
    public static void FireAndForgetSafeAsync(
        this Task task,
        IFireAndForgetTaskRunner runner,
        ILogger logger,
        [CallerMemberName] string callerName = "")
    {
        runner.RunAsync(task, logger, callerName);
    }
}
