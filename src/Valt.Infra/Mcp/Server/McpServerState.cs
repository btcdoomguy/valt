using CommunityToolkit.Mvvm.ComponentModel;

namespace Valt.Infra.Mcp.Server;

/// <summary>
/// Observable state for the MCP server, allowing the UI to react to server status changes.
/// </summary>
public partial class McpServerState : ObservableObject
{
    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _port = 5200;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _serverUrl;

    public void SetRunning(int port)
    {
        Port = port;
        ServerUrl = $"http://localhost:{port}/mcp";
        ErrorMessage = null;
        IsRunning = true;
    }

    public void SetStopped()
    {
        IsRunning = false;
        ServerUrl = null;
        ErrorMessage = null;
    }

    public void SetError(string message)
    {
        IsRunning = false;
        ServerUrl = null;
        ErrorMessage = message;
    }
}
