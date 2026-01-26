using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class DcaGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    [ObservableProperty]
    private int _targetPurchaseCount = 4;

    public string Description => language.GoalType_Dca_Description;

    public DcaGoalTypeEditorViewModel()
    {
    }

    public DcaGoalTypeEditorViewModel(int initialPurchaseCount)
    {
        TargetPurchaseCount = initialPurchaseCount;
    }

    public IGoalType CreateGoalType()
    {
        return new DcaGoalType(TargetPurchaseCount);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is DcaGoalType dca)
        {
            return new DcaGoalType(TargetPurchaseCount, dca.CalculatedPurchaseCount);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is DcaGoalType dca)
        {
            TargetPurchaseCount = dca.TargetPurchaseCount;
        }
    }

    public GoalTypeInputDTO CreateGoalTypeDTO()
    {
        return new DcaGoalTypeDTO { TargetPurchaseCount = TargetPurchaseCount };
    }

    public void LoadFromDTO(GoalTypeOutputDTO goalType)
    {
        if (goalType is DcaGoalTypeOutputDTO dca)
        {
            TargetPurchaseCount = dca.TargetPurchaseCount;
        }
    }
}
