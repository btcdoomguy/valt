using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Settings;

public partial class DisplaySettings : BaseSettings
{
    public DisplaySettings(ILocalDatabase localDatabase, INotificationPublisher notificationPublisher)
        : base(localDatabase, notificationPublisher)
    {
    }
    
    private bool _showHiddenAccounts;
    [PersistableSetting]
    public bool ShowHiddenAccounts
    {
        get => _showHiddenAccounts;
        set => SetProperty(ref _showHiddenAccounts, value);
    }

    private int _mcpServerPort = 5200;
    [PersistableSetting]
    public int McpServerPort
    {
        get => _mcpServerPort;
        set => SetProperty(ref _mcpServerPort, Math.Clamp(value, 1024, 65535));
    }

    protected sealed override void LoadDefaults()
    {
        ShowHiddenAccounts = false;
        McpServerPort = 5200;
    }
}