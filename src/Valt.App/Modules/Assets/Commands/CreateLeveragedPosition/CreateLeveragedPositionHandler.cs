using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;

internal sealed class CreateLeveragedPositionHandler : ICommandHandler<CreateLeveragedPositionCommand, CreateLeveragedPositionResult>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<CreateLeveragedPositionCommand> _validator;

    public CreateLeveragedPositionHandler(
        IAssetRepository assetRepository,
        IValidator<CreateLeveragedPositionCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<CreateLeveragedPositionResult>> HandleAsync(
        CreateLeveragedPositionCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateLeveragedPositionResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Validate currency
        try
        {
            FiatCurrency.GetFromCode(command.CurrencyCode);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<CreateLeveragedPositionResult>.Failure(
                "INVALID_CURRENCY",
                $"Invalid currency code: {command.CurrencyCode}");
        }

        var assetName = new AssetName(command.Name);
        var priceSource = (AssetPriceSource)command.PriceSource;

        var details = new LeveragedPositionDetails(
            collateral: command.Collateral,
            entryPrice: command.EntryPrice,
            leverage: command.Leverage,
            liquidationPrice: command.LiquidationPrice,
            currentPrice: command.CurrentPrice,
            currencyCode: command.CurrencyCode,
            symbol: command.Symbol,
            priceSource: priceSource,
            isLong: command.IsLong);

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

        return Result<CreateLeveragedPositionResult>.Success(
            new CreateLeveragedPositionResult(asset.Id.Value));
    }
}
