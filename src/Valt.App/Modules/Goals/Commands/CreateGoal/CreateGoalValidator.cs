using Valt.App.Kernel.Validation;
using Valt.App.Modules.Goals.Validation;

namespace Valt.App.Modules.Goals.Commands.CreateGoal;

public class CreateGoalValidator : IValidator<CreateGoalCommand>
{
    public ValidationResult Validate(CreateGoalCommand instance)
    {
        var builder = new ValidationResultBuilder();

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
