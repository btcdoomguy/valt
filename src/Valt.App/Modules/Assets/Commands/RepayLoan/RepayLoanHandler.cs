using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.RepayLoan;

internal sealed class RepayLoanHandler : ICommandHandler<RepayLoanCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<RepayLoanCommand> _validator;

    public RepayLoanHandler(
        IAssetRepository assetRepository,
        IValidator<RepayLoanCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(
        RepayLoanCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.NotFound("Asset", command.AssetId);

        IAssetDetails updatedDetails = asset.Details switch
        {
            BtcLoanDetails btcLoan => btcLoan.WithStatus(LoanStatus.Repaid),
            BtcLendingDetails btcLending => btcLending.WithStatus(LoanStatus.Repaid),
            _ => throw new InvalidOperationException($"Asset {command.AssetId} is not a loan or lending position")
        };

        asset.Edit(asset.Name, updatedDetails, asset.Icon, asset.IncludeInNetWorth, asset.Visible);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
