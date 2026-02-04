using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.SetAssetIncludeInNetWorth;

internal sealed class SetAssetIncludeInNetWorthValidator : IValidator<SetAssetIncludeInNetWorthCommand>
{
    public ValidationResult Validate(SetAssetIncludeInNetWorthCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        return builder.Build();
    }
}
