using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.UpdateAssetQuantity;

internal sealed class UpdateAssetQuantityValidator : IValidator<UpdateAssetQuantityCommand>
{
    public ValidationResult Validate(UpdateAssetQuantityCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        if (instance.NewQuantity < 0)
            builder.AddError(nameof(instance.NewQuantity), "New quantity cannot be negative.");

        return builder.Build();
    }
}
