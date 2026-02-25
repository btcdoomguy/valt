using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.CreateBtcLoan;

internal sealed class CreateBtcLoanHandler : ICommandHandler<CreateBtcLoanCommand, CreateBtcLoanResult>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<CreateBtcLoanCommand> _validator;

    public CreateBtcLoanHandler(
        IAssetRepository assetRepository,
        IValidator<CreateBtcLoanCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<CreateBtcLoanResult>> HandleAsync(
        CreateBtcLoanCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateBtcLoanResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Validate currency
        try
        {
            FiatCurrency.GetFromCode(command.CurrencyCode);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<CreateBtcLoanResult>.Failure(
                "INVALID_CURRENCY",
                $"Invalid currency code: {command.CurrencyCode}");
        }

        var assetName = new AssetName(command.Name);

        var details = new BtcLoanDetails(
            platformName: command.PlatformName,
            collateralSats: command.CollateralSats,
            loanAmount: command.LoanAmount,
            currencyCode: command.CurrencyCode,
            apr: command.Apr,
            initialLtv: command.InitialLtv,
            liquidationLtv: command.LiquidationLtv,
            marginCallLtv: command.MarginCallLtv,
            fees: command.Fees,
            loanStartDate: command.LoanStartDate,
            repaymentDate: command.RepaymentDate,
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: command.CurrentBtcPrice);

        var icon = string.IsNullOrWhiteSpace(command.Icon)
            ? Icon.Empty
            : Icon.RestoreFromId(command.Icon);

        var asset = Asset.New(
            assetName,
            details,
            icon,
            command.IncludeInNetWorth,
            command.Visible);

        await _assetRepository.SaveAsync(asset);

        return Result<CreateBtcLoanResult>.Success(
            new CreateBtcLoanResult(asset.Id.Value));
    }
}
