using Valt.Infra.DataAccess;

namespace Valt.Infra.Settings;

public partial class DisplaySettings : BaseSettings
{
    public DisplaySettings(ILocalDatabase localDatabase) : base(localDatabase)
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