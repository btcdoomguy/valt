using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.UpdateAssetQuantity;

internal sealed class UpdateAssetQuantityHandler : ICommandHandler<UpdateAssetQuantityCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<UpdateAssetQuantityCommand> _validator;

    public UpdateAssetQuantityHandler(
        IAssetRepository assetRepository,
        IValidator<UpdateAssetQuantityCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(
        UpdateAssetQuantityCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.NotFound("Asset", command.AssetId);

        if (asset.Details is not BasicAssetDetails basicDetails)
            return Result<Unit>.Failure(
                "INVALID_ASSET_TYPE",
                "Cannot update quantity on non-basic assets. Only Stock, ETF, Crypto, Commodity, and Custom assets support quantity updates.");

        var newDetails = basicDetails.WithQuantity(command.NewQuantity);
        asset.Edit(asset.Name, newDetails, asset.Icon, asset.IncludeInNetWorth, asset.Visible);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
