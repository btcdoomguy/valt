using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Valt.UI.Base;

/// <summary>
/// Default implementation of <see cref="IFireAndForgetTaskRunner"/>.
/// Starts the task on the thread pool and logs the outcome through the supplied logger.
/// </summary>
public sealed class FireAndForgetTaskRunner : IFireAndForgetTaskRunner
{
    /// <inheritdoc />
    public void RunAsync(
        Task task,
        ILogger logger,
        [CallerMemberName] string callerName = "")
    {
        _ = ObserveAsync(task, logger, callerName);
    }

    private static async Task ObserveAsync(
        Task task,
        ILogger logger,
        string callerName)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug(
                "Fire-and-forget task cancelled in {CallerName}",
                callerName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Fire-and-forget task failed in {CallerName}",
                callerName);
        }
    }
}
