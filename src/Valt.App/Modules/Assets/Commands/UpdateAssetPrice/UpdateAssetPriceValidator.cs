using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.UpdateAssetPrice;

internal sealed class UpdateAssetPriceValidator : IValidator<UpdateAssetPriceCommand>
{
    public ValidationResult Validate(UpdateAssetPriceCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        if (instance.NewPrice < 0)
            builder.AddError(nameof(instance.NewPrice), "New price cannot be negative.");

        return builder.Build();
    }
}
