using Valt.App.Kernel.Validation;
using Valt.Core.Kernel.Abstractions.Time;

namespace Valt.App.Modules.Assets.Commands.MarkAssetAsSold;

internal sealed class MarkAssetAsSoldValidator : IValidator<MarkAssetAsSoldCommand>
{
    private readonly IClock _clock;

    public MarkAssetAsSoldValidator(IClock clock)
    {
        _clock = clock;
    }

    public ValidationResult Validate(MarkAssetAsSoldCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        if (instance.DateSold.HasValue && instance.DateSold.Value > _clock.GetCurrentLocalDate())
        {
            builder.AddError(nameof(instance.DateSold), "Date Sold cannot be in the future.");
        }

        return builder.Build();
    }
}
