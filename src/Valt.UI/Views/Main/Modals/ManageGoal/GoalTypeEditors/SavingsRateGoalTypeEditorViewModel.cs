using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class SavingsRateGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    [ObservableProperty]
    private decimal _targetPercentage;

    public string Description => language.GoalType_SavingsRate_Description;

    public SavingsRateGoalTypeEditorViewModel()
    {
    }

    public IGoalType CreateGoalType()
    {
        return new SavingsRateGoalType(TargetPercentage);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is SavingsRateGoalType savingsRate)
        {
            return new SavingsRateGoalType(TargetPercentage, savingsRate.CalculatedPercentage);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is SavingsRateGoalType savingsRate)
        {
            TargetPercentage = savingsRate.TargetPercentage;
        }
    }

    public GoalTypeInputDTO CreateGoalTypeDTO()
    {
        return new SavingsRateGoalTypeDTO { TargetPercentage = TargetPercentage };
    }

    public void LoadFromDTO(GoalTypeOutputDTO goalType)
    {
        if (goalType is SavingsRateGoalTypeOutputDTO savingsRate)
        {
            TargetPercentage = savingsRate.TargetPercentage;
        }
    }
}
