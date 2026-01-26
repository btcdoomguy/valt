using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class IncomeBtcGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    [ObservableProperty]
    private BtcValue _targetBtcAmount = BtcValue.Empty;

    public string Description => language.GoalType_IncomeBtc_Description;

    public IncomeBtcGoalTypeEditorViewModel()
    {
    }

    public IncomeBtcGoalTypeEditorViewModel(BtcValue initialAmount)
    {
        TargetBtcAmount = initialAmount;
    }

    public IGoalType CreateGoalType()
    {
        return new IncomeBtcGoalType(TargetBtcAmount);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is IncomeBtcGoalType incomeBtc)
        {
            return new IncomeBtcGoalType(TargetBtcAmount.Sats, incomeBtc.CalculatedSats);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is IncomeBtcGoalType incomeBtc)
        {
            TargetBtcAmount = incomeBtc.TargetAmount;
        }
    }

    public GoalTypeInputDTO CreateGoalTypeDTO()
    {
        return new IncomeBtcGoalTypeDTO { TargetSats = TargetBtcAmount.Sats };
    }

    public void LoadFromDTO(GoalTypeOutputDTO goalType)
    {
        if (goalType is IncomeBtcGoalTypeOutputDTO incomeBtc)
        {
            TargetBtcAmount = incomeBtc.TargetSats;
        }
    }
}
