using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.UndoAssetSale;

internal sealed class UndoAssetSaleValidator : IValidator<UndoAssetSaleCommand>
{
    public ValidationResult Validate(UndoAssetSaleCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        return builder.Build();
    }
}
