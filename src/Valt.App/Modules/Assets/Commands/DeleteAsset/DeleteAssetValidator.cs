using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.DeleteAsset;

internal sealed class DeleteAssetValidator : IValidator<DeleteAssetCommand>
{
    public ValidationResult Validate(DeleteAssetCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        return builder.Build();
    }
}
