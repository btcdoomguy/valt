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

    protected sealed override void LoadDefaults()
    {
        ShowHiddenAccounts = false;
    }
}