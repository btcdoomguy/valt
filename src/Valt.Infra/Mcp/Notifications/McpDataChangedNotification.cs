using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Mcp.Notifications;

/// <summary>
/// Notification published when an MCP tool performs a write operation.
/// </summary>
public record McpDataChangedNotification() : INotification;
