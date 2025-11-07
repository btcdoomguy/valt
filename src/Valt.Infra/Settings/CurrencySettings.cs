using Valt.Core.Common;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Settings;

public partial class CurrencySettings(ILocalDatabase localDatabase) : BaseSettings(localDatabase)
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