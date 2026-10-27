using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Budget.Transactions.Commands.SetTransactionExclusion;

internal sealed class SetTransactionExclusionValidator : IValidator<SetTransactionExclusionCommand>
{
    public ValidationResult Validate(SetTransactionExclusionCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.TransactionId, nameof(command.TransactionId),
            "Transaction ID is required.");

        // Guard the repository's new ObjectId(id) call, which throws FormatException for
        // non-24-hex-char strings (reachable via the free-form MCP tool parameter).
        if (!string.IsNullOrWhiteSpace(command.TransactionId) && !IsValidIdFormat(command.TransactionId))
            builder.AddError(nameof(command.TransactionId), "Transaction ID must be a valid ID.");

        return builder.Build();
    }

    // Mirrors LiteDB ObjectId.TryParse semantics (exactly 24 hex chars) without a LiteDB
    // dependency in the App layer. Same approach as GoalContributingTransactionsService.TryParseObjectId.
    private static bool IsValidIdFormat(string value)
    {
        if (value.Length != 24)
            return false;

        foreach (var c in value)
        {
            var isHex = c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
            if (!isHex)
                return false;
        }

        return true;
    }
}
