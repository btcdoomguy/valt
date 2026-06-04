using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Validation;

public static class TransactionValidationRules
{
    public const int MaxNameLength = 60;
    public const int MaxNotesLength = 500;

    public static void ValidateCommonFields(string? name, string? categoryId, string? notes, string? fixedExpenseId, DateOnly? fixedExpenseReferenceDate, ValidationResultBuilder builder)
    {
        builder.AddErrorIfNullOrWhiteSpace(name, nameof(name), "Transaction name is required.");

        if (!string.IsNullOrWhiteSpace(name) && name.Length > MaxNameLength)
        {
            builder.AddError(nameof(name), $"Transaction name cannot exceed {MaxNameLength} characters.");
        }

        builder.AddErrorIfNullOrWhiteSpace(categoryId, nameof(categoryId), "Category is required.");

        if (!string.IsNullOrEmpty(notes) && notes.Length > MaxNotesLength)
        {
            builder.AddError(nameof(notes), $"Notes cannot exceed {MaxNotesLength} characters.");
        }

        if (!string.IsNullOrEmpty(fixedExpenseId) && !fixedExpenseReferenceDate.HasValue)
        {
            builder.AddError(nameof(fixedExpenseReferenceDate),
                "Fixed expense reference date is required when fixed expense is specified.");
        }
    }

    public static void ValidateDetails(TransactionDetailsDto details, ValidationResultBuilder builder)
    {
        builder.AddErrorIfNullOrWhiteSpace(
            details.FromAccountId,
            nameof(details.FromAccountId),
            "From account is required.");

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
