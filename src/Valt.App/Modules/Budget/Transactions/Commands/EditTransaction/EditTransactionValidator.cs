using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Commands.EditTransaction;

internal sealed class EditTransactionValidator : IValidator<EditTransactionCommand>
{
    private const int MaxNameLength = 60;
    private const int MaxNotesLength = 500;

    public ValidationResult Validate(EditTransactionCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.TransactionId, nameof(command.TransactionId),
            "Transaction ID is required.");

        builder.AddErrorIfNullOrWhiteSpace(command.Name, nameof(command.Name), "Transaction name is required.");

        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name.Length > MaxNameLength)
        {
            builder.AddError(nameof(command.Name), $"Transaction name cannot exceed {MaxNameLength} characters.");
        }

        builder.AddErrorIfNullOrWhiteSpace(command.CategoryId, nameof(command.CategoryId), "Category is required.");

        if (command.Details is null)
        {
            builder.AddError(nameof(command.Details), "Transaction details are required.");
        }
        else
        {
            builder.AddErrorIfNullOrWhiteSpace(
                command.Details.FromAccountId,
                nameof(command.Details.FromAccountId),
                "From account is required.");

            ValidateDetails(command.Details, builder);
        }

        if (!string.IsNullOrEmpty(command.Notes) && command.Notes.Length > MaxNotesLength)
        {
            builder.AddError(nameof(command.Notes), $"Notes cannot exceed {MaxNotesLength} characters.");
        }

        if (!string.IsNullOrEmpty(command.FixedExpenseId) && !command.FixedExpenseReferenceDate.HasValue)
        {
            builder.AddError(nameof(command.FixedExpenseReferenceDate),
                "Fixed expense reference date is required when fixed expense is specified.");
        }

        return builder.Build();
    }

    private static void ValidateDetails(TransactionDetailsDto details, ValidationResultBuilder builder)
    {
        switch (details)
        {
            case FiatTransactionDto fiat:
                if (fiat.Amount <= 0)
                {
                    builder.AddError(nameof(FiatTransactionDto.Amount), "Amount must be greater than zero.");
                }
                break;

            case BitcoinTransactionDto btc:
                if (btc.AmountSats <= 0)
                {
                    builder.AddError(nameof(BitcoinTransactionDto.AmountSats), "Amount must be greater than zero.");
                }
                break;

            case FiatToFiatTransferDto fiatToFiat:
                builder.AddErrorIfNullOrWhiteSpace(fiatToFiat.ToAccountId, "ToAccountId", "To account is required.");
                if (fiatToFiat.FromAmount <= 0)
                {
                    builder.AddError(nameof(FiatToFiatTransferDto.FromAmount), "From amount must be greater than zero.");
                }
                if (fiatToFiat.ToAmount <= 0)
                {
                    builder.AddError(nameof(FiatToFiatTransferDto.ToAmount), "To amount must be greater than zero.");
                }
                if (fiatToFiat.FromAccountId == fiatToFiat.ToAccountId)
                {
                    builder.AddError(nameof(FiatToFiatTransferDto.ToAccountId),
                        "From and To accounts must be different.");
                }
                break;

            case BitcoinToBitcoinTransferDto btcToBtc:
                builder.AddErrorIfNullOrWhiteSpace(btcToBtc.ToAccountId, "ToAccountId", "To account is required.");
                if (btcToBtc.AmountSats <= 0)
                {
                    builder.AddError(nameof(BitcoinToBitcoinTransferDto.AmountSats), "Amount must be greater than zero.");
                }
                if (btcToBtc.FromAccountId == btcToBtc.ToAccountId)
                {
                    builder.AddError(nameof(BitcoinToBitcoinTransferDto.ToAccountId),
                        "From and To accounts must be different.");
                }
                break;

            case FiatToBitcoinTransferDto fiatToBtc:
                builder.AddErrorIfNullOrWhiteSpace(fiatToBtc.ToAccountId, "ToAccountId", "To account is required.");
                if (fiatToBtc.FromFiatAmount <= 0)
                {
                    builder.AddError(nameof(FiatToBitcoinTransferDto.FromFiatAmount),
                        "From amount must be greater than zero.");
                }
                if (fiatToBtc.ToSatsAmount <= 0)
                {
                    builder.AddError(nameof(FiatToBitcoinTransferDto.ToSatsAmount),
                        "To amount must be greater than zero.");
                }
                break;

            case BitcoinToFiatTransferDto btcToFiat:
                builder.AddErrorIfNullOrWhiteSpace(btcToFiat.ToAccountId, "ToAccountId", "To account is required.");
                if (btcToFiat.FromSatsAmount <= 0)
                {
                    builder.AddError(nameof(BitcoinToFiatTransferDto.FromSatsAmount),
                        "From amount must be greater than zero.");
                }
                if (btcToFiat.ToFiatAmount <= 0)
                {
                    builder.AddError(nameof(BitcoinToFiatTransferDto.ToFiatAmount),
                        "To amount must be greater than zero.");
                }
                break;
        }
    }
}
