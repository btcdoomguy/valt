using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;

internal sealed class CreateRealEstateAssetValidator : IValidator<CreateRealEstateAssetCommand>
{
    private const int MaxNameLength = 100;
    private const int MaxAddressLength = 500;

    public ValidationResult Validate(CreateRealEstateAssetCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.Name, nameof(instance.Name), "Property name is required.");

        if (instance.Name?.Length > MaxNameLength)
            builder.AddError(nameof(instance.Name), $"Property name cannot exceed {MaxNameLength} characters.");

        builder.AddErrorIfNullOrWhiteSpace(instance.CurrencyCode, nameof(instance.CurrencyCode), "Currency code is required.");

        if (instance.CurrentValue < 0)
            builder.AddError(nameof(instance.CurrentValue), "Current value cannot be negative.");

        if (instance.Address?.Length > MaxAddressLength)
            builder.AddError(nameof(instance.Address), $"Address cannot exceed {MaxAddressLength} characters.");

        if (instance.MonthlyRentalIncome.HasValue && instance.MonthlyRentalIncome.Value < 0)
            builder.AddError(nameof(instance.MonthlyRentalIncome), "Monthly rental income cannot be negative.");

        return builder.Build();
    }
}
