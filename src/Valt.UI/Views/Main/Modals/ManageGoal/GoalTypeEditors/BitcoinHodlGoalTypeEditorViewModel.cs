using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class BitcoinHodlGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    [ObservableProperty]
    private BtcValue _maxSellableBtcAmount = BtcValue.Empty;

    public string Description => language.GoalType_BitcoinHodl_Description;

    public BitcoinHodlGoalTypeEditorViewModel()
    {
    }

    public IGoalType CreateGoalType()
    {
        return new BitcoinHodlGoalType(MaxSellableBtcAmount.Sats);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is BitcoinHodlGoalType bitcoinHodl)
        {
            return new BitcoinHodlGoalType(MaxSellableBtcAmount.Sats, bitcoinHodl.CalculatedSoldSats);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is BitcoinHodlGoalType bitcoinHodl)
        {
            MaxSellableBtcAmount = bitcoinHodl.MaxSellableSats;
        }
    }

    public GoalTypeInputDTO CreateGoalTypeDTO()
    {
        return new BitcoinHodlGoalTypeDTO { MaxSellableSats = MaxSellableBtcAmount.Sats };
    }

    public void LoadFromDTO(GoalTypeOutputDTO goalType)
    {
        if (goalType is BitcoinHodlGoalTypeOutputDTO hodl)
        {
            MaxSellableBtcAmount = hodl.MaxSellableSats;
        }
    }
}
