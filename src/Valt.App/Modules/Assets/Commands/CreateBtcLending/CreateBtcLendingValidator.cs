using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.CreateBtcLending;

internal sealed class CreateBtcLendingValidator : IValidator<CreateBtcLendingCommand>
{
    private const int MaxNameLength = 100;

    public ValidationResult Validate(CreateBtcLendingCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.Name, nameof(instance.Name), "Lending name is required.");

        if (instance.Name?.Length > MaxNameLength)
            builder.AddError(nameof(instance.Name), $"Lending name cannot exceed {MaxNameLength} characters.");

        builder.AddErrorIfNullOrWhiteSpace(instance.CurrencyCode, nameof(instance.CurrencyCode), "Currency code is required.");

        builder.AddErrorIfNullOrWhiteSpace(instance.BorrowerOrPlatformName, nameof(instance.BorrowerOrPlatformName), "Borrower or platform name is required.");

        if (instance.AmountLent <= 0)
            builder.AddError(nameof(instance.AmountLent), "Amount lent must be greater than zero.");

        if (instance.Apr < 0)
            builder.AddError(nameof(instance.Apr), "APR cannot be negative.");

        return builder.Build();
    }
}
