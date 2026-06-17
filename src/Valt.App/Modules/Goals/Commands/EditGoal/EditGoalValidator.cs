using Valt.App.Kernel.Validation;
using Valt.App.Modules.Goals.Validation;

namespace Valt.App.Modules.Goals.Commands.EditGoal;

public class EditGoalValidator : IValidator<EditGoalCommand>
{
    public ValidationResult Validate(EditGoalCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.GoalId, nameof(instance.GoalId), "Goal ID is required");

        GoalValidationRules.ValidateCommonFields(instance.Period, instance.StartDate, instance.RefDate, builder);

        if (instance.GoalType is null)
        {
            builder.AddError(nameof(instance.GoalType), "Goal type is required");
        }
        else
        {
            GoalValidationRules.ValidateGoalType(instance.GoalType, builder);
        }

        return builder.Build();
    }
}
