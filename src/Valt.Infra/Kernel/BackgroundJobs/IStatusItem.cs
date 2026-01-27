using System.ComponentModel;

namespace Valt.Infra.Kernel.BackgroundJobs;

/// <summary>
/// Interface for items displayed in the StatusDisplay modal.
/// Implemented by both JobInfo (background jobs) and McpServerState.
/// </summary>
public interface IStatusItem : INotifyPropertyChanged
{
    /// <summary>
    /// Display name of the status item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable state display (e.g., "Running", "Stopped", "Error").
    /// </summary>
    string StateDisplay { get; }

    /// <summary>
    /// Log pool for viewing item logs.
    /// </summary>
    JobLogPool LogPool { get; }
}
