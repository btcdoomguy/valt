using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class StackBitcoinGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    [ObservableProperty]
    private BtcValue _targetBtcAmount = BtcValue.Empty;

    public string Description => language.GoalType_StackBitcoin_Description;

    public StackBitcoinGoalTypeEditorViewModel()
    {
    }

    public StackBitcoinGoalTypeEditorViewModel(BtcValue initialAmount)
    {
        TargetBtcAmount = initialAmount;
    }

    public IGoalType CreateGoalType()
    {
        return new StackBitcoinGoalType(TargetBtcAmount);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is StackBitcoinGoalType stackBitcoin)
        {
            return new StackBitcoinGoalType(TargetBtcAmount.Sats, stackBitcoin.CalculatedSats);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is StackBitcoinGoalType stackBitcoin)
        {
            TargetBtcAmount = stackBitcoin.TargetAmount;
        }
    }
}
