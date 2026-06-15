using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.AddLoanStateUpdate;

internal sealed class AddLoanStateUpdateHandler : ICommandHandler<AddLoanStateUpdateCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<AddLoanStateUpdateCommand> _validator;

    public AddLoanStateUpdateHandler(
        IAssetRepository assetRepository,
        IValidator<AddLoanStateUpdateCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(
        AddLoanStateUpdateCommand command,
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
                "Cannot add loan state update on non-BTC loan assets. Only BTC-backed loans support state snapshots.");

        var latestSnapshot = btcLoan.Snapshots.Count == 0
            ? null
            : btcLoan.Snapshots.MaxBy(s => s.EffectiveDate);

        var latestEffectiveDate = latestSnapshot?.EffectiveDate ?? btcLoan.LoanStartDate;
        if (command.EffectiveDate <= latestEffectiveDate)
            return Result<Unit>.Failure(
                "VALIDATION_FAILED",
                "Effective date must be after the latest snapshot.");

        var source = latestSnapshot ?? new LoanStateSnapshot(
            platformName: btcLoan.PlatformName,
            collateralSats: btcLoan.CollateralSats,
            loanAmount: btcLoan.LoanAmount,
            currencyCode: btcLoan.CurrencyCode,
            apr: btcLoan.Apr,
            initialLtv: btcLoan.InitialLtv,
            liquidationLtv: btcLoan.LiquidationLtv,
            marginCallLtv: btcLoan.MarginCallLtv,
            fees: btcLoan.Fees,
            loanStartDate: btcLoan.LoanStartDate,
            repaymentDate: btcLoan.RepaymentDate,
            status: btcLoan.Status,
            currentBtcPriceInLoanCurrency: btcLoan.CurrentBtcPriceInLoanCurrency,
            fixedTotalDebt: btcLoan.FixedTotalDebt,
            currentTotalDebt: btcLoan.CalculateTotalDebt(),
            effectiveDate: btcLoan.LoanStartDate,
            note: null);

        var snapshot = new LoanStateSnapshot(
            platformName: source.PlatformName,
            collateralSats: command.CollateralSats,
            loanAmount: source.LoanAmount,
            currencyCode: source.CurrencyCode,
            apr: command.Apr,
            initialLtv: source.InitialLtv,
            liquidationLtv: source.LiquidationLtv,
            marginCallLtv: source.MarginCallLtv,
            fees: command.Fees,
            loanStartDate: source.LoanStartDate,
            repaymentDate: source.RepaymentDate,
            status: source.Status,
            currentBtcPriceInLoanCurrency: source.CurrentBtcPriceInLoanCurrency,
            fixedTotalDebt: source.FixedTotalDebt,
            currentTotalDebt: command.CurrentTotalDebt,
            effectiveDate: command.EffectiveDate,
            note: command.Note);

        var newDetails = btcLoan.WithAddedSnapshot(snapshot);
        asset.Edit(asset.Name, newDetails, asset.Icon, asset.IncludeInNetWorth, asset.Visible);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
