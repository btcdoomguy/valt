using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.CreateBtcLending;

internal sealed class CreateBtcLendingHandler : ICommandHandler<CreateBtcLendingCommand, CreateBtcLendingResult>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<CreateBtcLendingCommand> _validator;

    public CreateBtcLendingHandler(
        IAssetRepository assetRepository,
        IValidator<CreateBtcLendingCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<CreateBtcLendingResult>> HandleAsync(
        CreateBtcLendingCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateBtcLendingResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Validate currency
        try
        {
            FiatCurrency.GetFromCode(command.CurrencyCode);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<CreateBtcLendingResult>.Failure(
                "INVALID_CURRENCY",
                $"Invalid currency code: {command.CurrencyCode}");
        }

        var assetName = new AssetName(command.Name);

        var details = new BtcLendingDetails(
            amountLent: command.AmountLent,
            currencyCode: command.CurrencyCode,
            apr: command.Apr,
            expectedRepaymentDate: command.ExpectedRepaymentDate,
            borrowerOrPlatformName: command.BorrowerOrPlatformName,
            lendingStartDate: command.LendingStartDate,
            status: LoanStatus.Active);

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

        return Result<CreateBtcLendingResult>.Success(
            new CreateBtcLendingResult(asset.Id.Value));
    }
}
