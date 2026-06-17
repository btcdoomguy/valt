using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.Transactions.Validation;

namespace Valt.App.Modules.Budget.Transactions.Commands.AddTransaction;

internal sealed class AddTransactionValidator : IValidator<AddTransactionCommand>
{
    public ValidationResult Validate(AddTransactionCommand command)
    {
        var builder = new ValidationResultBuilder();

        TransactionValidationRules.ValidateCommonFields(
            command.Name,
            command.CategoryId,
            command.Notes,
            command.FixedExpenseId,
            command.FixedExpenseReferenceDate,
            builder);

        if (command.Details is null)
        {
            builder.AddError(nameof(command.Details), "Transaction details are required.");
        }
        else
        {
            TransactionValidationRules.ValidateDetails(command.Details, builder);
        }

        return builder.Build();
    }
}
