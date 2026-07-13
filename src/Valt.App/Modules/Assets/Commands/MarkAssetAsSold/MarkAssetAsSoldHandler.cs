using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;

namespace Valt.App.Modules.Assets.Commands.MarkAssetAsSold;

internal sealed class MarkAssetAsSoldHandler : ICommandHandler<MarkAssetAsSoldCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<MarkAssetAsSoldCommand> _validator;
    private readonly IClock _clock;

    public MarkAssetAsSoldHandler(
        IAssetRepository assetRepository,
        IValidator<MarkAssetAsSoldCommand> validator,
        IClock clock)
    {
        _assetRepository = assetRepository;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<Unit>> HandleAsync(
        MarkAssetAsSoldCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.NotFound("Asset", command.AssetId);

        var effectiveDate = command.DateSold ?? _clock.GetCurrentLocalDate();
        asset.MarkAsSold(effectiveDate);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
