using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Goals;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public interface IGoalTypeEditorViewModel
{
    IGoalType CreateGoalType();
    IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing);
    void LoadFrom(IGoalType goalType);

    // DTO-based methods for App layer integration
    GoalTypeInputDTO CreateGoalTypeDTO();
    void LoadFromDTO(GoalTypeOutputDTO goalType);
}
