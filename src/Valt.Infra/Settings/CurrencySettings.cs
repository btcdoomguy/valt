using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Settings;

public partial class CurrencySettings(ILocalDatabase localDatabase, INotificationPublisher notificationPublisher)
    : BaseSettings(localDatabase, notificationPublisher)
{
    private string _mainFiatCurrency = FiatCurrency.Usd.Code;

    [PersistableSetting]
    public string MainFiatCurrency
    {
        get => _mainFiatCurrency;
        set => SetProperty(ref _mainFiatCurrency, value);
    }

    protected sealed override void LoadDefaults()
    {
        MainFiatCurrency = FiatCurrency.Usd.Code;
    }
}