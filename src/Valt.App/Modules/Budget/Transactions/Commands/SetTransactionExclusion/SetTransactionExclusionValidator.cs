using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Budget.Transactions.Commands.SetTransactionExclusion;

internal sealed class SetTransactionExclusionValidator : IValidator<SetTransactionExclusionCommand>
{
    public ValidationResult Validate(SetTransactionExclusionCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.TransactionId, nameof(command.TransactionId),
            "Transaction ID is required.");

        return builder.Build();
    }
}
