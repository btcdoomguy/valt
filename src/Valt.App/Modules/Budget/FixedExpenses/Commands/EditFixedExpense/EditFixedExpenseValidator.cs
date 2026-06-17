using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.FixedExpenses.Validation;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.EditFixedExpense;

public class EditFixedExpenseValidator : IValidator<EditFixedExpenseCommand>
{
    public ValidationResult Validate(EditFixedExpenseCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.FixedExpenseId, nameof(instance.FixedExpenseId), "Fixed expense ID is required");

        FixedExpenseValidationRules.ValidateCommonFields(
            instance.Name,
            instance.CategoryId,
            instance.DefaultAccountId,
            instance.Currency,
            builder);

        if (instance.NewRange is not null)
        {
            FixedExpenseValidationRules.ValidateRange(instance.NewRange, nameof(instance.NewRange), builder);
        }

        return builder.Build();
    }
}
