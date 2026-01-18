using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Valt.UI.Base;

/// <summary>
/// Extension methods for safe fire-and-forget task handling
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Safely executes a task without awaiting, with error handling via optional callback or logger.
    /// Use this instead of `_ = SomeAsync()` to ensure exceptions are properly logged.
    /// </summary>
    /// <param name="task">The task to execute</param>
    /// <param name="onError">Optional callback for error handling</param>
    /// <param name="logger">Optional logger for error logging</param>
    /// <param name="callerName">Name of the calling method for logging context</param>
    public static async void SafeFireAndForget(
        this Task task,
        Action<Exception>? onError = null,
        ILogger? logger = null,
        string? callerName = null)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation - this is expected behavior
        }
        catch (Exception ex)
        {
            if (onError is not null)
            {
                onError(ex);
            }
            else if (logger is not null)
            {
                logger.LogError(ex, "Fire-and-forget task failed in {CallerName}", callerName ?? "unknown");
            }
            else
            {
                // At minimum, write to debug output so the error isn't completely swallowed
                System.Diagnostics.Debug.WriteLine($"[FireAndForget Error] {callerName ?? "Unknown"}: {ex}");
            }
        }
    }

}
