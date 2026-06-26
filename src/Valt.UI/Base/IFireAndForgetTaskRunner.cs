using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Valt.UI.Base;

/// <summary>
/// Contract for executing tasks in a fire-and-forget manner with mandatory logging.
/// </summary>
public interface IFireAndForgetTaskRunner
{
    /// <summary>
    /// Observes the task without awaiting it. Cancellation is logged at Debug level;
    /// all other unhandled exceptions are logged at Error level.
    /// </summary>
    /// <param name="task">The task to observe.</param>
    /// <param name="logger">Logger used to record cancellation or failure.</param>
    /// <param name="callerName">Name of the calling method, captured automatically.</param>
    void RunAsync(
        Task task,
        ILogger logger,
        [CallerMemberName] string callerName = "");
}
