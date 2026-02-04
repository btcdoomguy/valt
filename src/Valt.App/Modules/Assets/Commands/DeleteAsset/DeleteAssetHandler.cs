using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;

namespace Valt.App.Modules.Assets.Commands.DeleteAsset;

internal sealed class DeleteAssetHandler : ICommandHandler<DeleteAssetCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<DeleteAssetCommand> _validator;

    public DeleteAssetHandler(
        IAssetRepository assetRepository,
        IValidator<DeleteAssetCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(
        DeleteAssetCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.NotFound("Asset", command.AssetId);

        await _assetRepository.DeleteAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
