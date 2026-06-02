using CommunityToolkit.Mvvm.ComponentModel;

namespace Valt.UI.State;

public partial class CustomBtcPriceState : ObservableObject
{
    [ObservableProperty] private decimal? _customBtcPriceUsd;
    
    public bool IsActive => CustomBtcPriceUsd.HasValue;
    
    public decimal GetEffectiveBtcPriceUsd(decimal liveBtcPriceUsd)
    {
        return CustomBtcPriceUsd ?? liveBtcPriceUsd;
    }
}