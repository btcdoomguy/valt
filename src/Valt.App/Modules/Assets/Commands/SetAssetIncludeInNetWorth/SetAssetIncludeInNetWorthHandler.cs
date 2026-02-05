using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;

namespace Valt.App.Modules.Assets.Commands.SetAssetIncludeInNetWorth;

internal sealed class SetAssetIncludeInNetWorthHandler : ICommandHandler<SetAssetIncludeInNetWorthCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<SetAssetIncludeInNetWorthCommand> _validator;

    public SetAssetIncludeInNetWorthHandler(
        IAssetRepository assetRepository,
        IValidator<SetAssetIncludeInNetWorthCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(
        SetAssetIncludeInNetWorthCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.NotFound("Asset", command.AssetId);

        asset.SetIncludeInNetWorth(command.IncludeInNetWorth);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
