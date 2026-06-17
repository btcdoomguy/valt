using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.Transactions.Validation;

namespace Valt.App.Modules.Budget.Transactions.Commands.EditTransaction;

internal sealed class EditTransactionValidator : IValidator<EditTransactionCommand>
{
    public ValidationResult Validate(EditTransactionCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.TransactionId, nameof(command.TransactionId),
            "Transaction ID is required.");

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
