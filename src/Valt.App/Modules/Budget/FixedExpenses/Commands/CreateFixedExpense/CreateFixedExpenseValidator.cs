using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.FixedExpenses.Validation;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;

public class CreateFixedExpenseValidator : IValidator<CreateFixedExpenseCommand>
{
    public ValidationResult Validate(CreateFixedExpenseCommand instance)
    {
        var builder = new ValidationResultBuilder();

        FixedExpenseValidationRules.ValidateCommonFields(
            instance.Name,
            instance.CategoryId,
            instance.DefaultAccountId,
            instance.Currency,
            builder);

        if (instance.Ranges.Count == 0)
        {
            builder.AddError(nameof(instance.Ranges), "At least one range is required");
        }
        else
        {
            for (int i = 0; i < instance.Ranges.Count; i++)
            {
                FixedExpenseValidationRules.ValidateRange(instance.Ranges[i], $"Ranges[{i}]", builder);
            }
        }

        return builder.Build();
    }
}
