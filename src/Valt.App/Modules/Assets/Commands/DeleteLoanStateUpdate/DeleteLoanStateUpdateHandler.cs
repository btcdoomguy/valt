using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.DeleteLoanStateUpdate;

internal sealed class DeleteLoanStateUpdateHandler : ICommandHandler<DeleteLoanStateUpdateCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<DeleteLoanStateUpdateCommand> _validator;

    public DeleteLoanStateUpdateHandler(
        IAssetRepository assetRepository,
        IValidator<DeleteLoanStateUpdateCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(
        DeleteLoanStateUpdateCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.NotFound("Asset", command.AssetId);

        if (asset.Details is not BtcLoanDetails btcLoan)
            return Result<Unit>.Failure(
                "INVALID_ASSET_TYPE",
                "Cannot delete loan state update on non-BTC loan assets. Only BTC-backed loans support state snapshots.");

        var orderedSnapshots = btcLoan.Snapshots.OrderBy(s => s.EffectiveDate).ToList();
        var initialSnapshot = orderedSnapshots.FirstOrDefault();
        if (initialSnapshot is not null && initialSnapshot.EffectiveDate == command.EffectiveDate)
            return Result<Unit>.Failure(
                "CANNOT_DELETE_INITIAL_SNAPSHOT",
                "Cannot delete the initial loan state snapshot.");

        var newDetails = btcLoan.WithoutSnapshot(command.EffectiveDate);
        asset.Edit(asset.Name, newDetails, asset.Icon, asset.IncludeInNetWorth, asset.Visible);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
