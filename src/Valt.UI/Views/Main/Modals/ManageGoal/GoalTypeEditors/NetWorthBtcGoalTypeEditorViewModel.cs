using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class NetWorthBtcGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    [ObservableProperty]
    private BtcValue _targetBtcAmount = BtcValue.Empty;

    public string Description => language.GoalType_NetWorthBtc_Description;

    public NetWorthBtcGoalTypeEditorViewModel()
    {
    }

    public NetWorthBtcGoalTypeEditorViewModel(BtcValue initialAmount)
    {
        TargetBtcAmount = initialAmount;
    }

    public IGoalType CreateGoalType()
    {
        return new NetWorthBtcGoalType(TargetBtcAmount.Sats);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is NetWorthBtcGoalType netWorthBtc)
        {
            return new NetWorthBtcGoalType(TargetBtcAmount.Sats, netWorthBtc.CalculatedSats);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is NetWorthBtcGoalType netWorthBtc)
        {
            TargetBtcAmount = BtcValue.ParseSats(netWorthBtc.TargetSats);
        }
    }

    public GoalTypeInputDTO CreateGoalTypeDTO()
    {
        return new NetWorthBtcGoalTypeDTO { TargetSats = TargetBtcAmount.Sats };
    }

    public void LoadFromDTO(GoalTypeOutputDTO goalType)
    {
        if (goalType is NetWorthBtcGoalTypeOutputDTO netWorthBtc)
        {
            TargetBtcAmount = BtcValue.ParseSats(netWorthBtc.TargetSats);
        }
    }
}
