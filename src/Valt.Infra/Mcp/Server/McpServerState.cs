using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Mcp.Server;

/// <summary>
/// Observable state for the MCP server, allowing the UI to react to server status changes.
/// </summary>
public partial class McpServerState : ObservableObject, IStatusItem
{
    private Timer? _activityTimer;
    private readonly Lock _timerLock = new();
    private readonly JobLogPool _logPool = new();

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _port = 5200;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _serverUrl;

    [ObservableProperty]
    private bool _isProcessing;

    // IStatusItem implementation
    public string Name => "MCP Server";
    public string StateDisplay => ErrorMessage != null ? "Error" : (IsRunning ? "Running" : "Stopped");
    public JobLogPool LogPool => _logPool;

    public void SetRunning(int port)
    {
        Port = port;
        ServerUrl = $"http://localhost:{port}/mcp";
        ErrorMessage = null;
        IsRunning = true;
        OnPropertyChanged(nameof(StateDisplay));
    }

    public void SetStopped()
    {
        IsRunning = false;
        ServerUrl = null;
        ErrorMessage = null;
        IsProcessing = false;
        OnPropertyChanged(nameof(StateDisplay));
    }

    public void SetError(string message)
    {
        IsRunning = false;
        ServerUrl = null;
        ErrorMessage = message;
        IsProcessing = false;
        OnPropertyChanged(nameof(StateDisplay));
    }

    /// <summary>
    /// Signals that MCP activity is happening (tool call).
    /// The processing indicator will turn on and automatically turn off after a short delay.
    /// </summary>
    public void SignalActivity()
    {
        lock (_timerLock)
        {
            IsProcessing = true;

            // Cancel any existing timer
            _activityTimer?.Dispose();

            // Set timer to turn off the indicator after 150ms
            _activityTimer = new Timer(_ =>
            {
                IsProcessing = false;
            }, null, 150, Timeout.Infinite);
        }
    }
}
