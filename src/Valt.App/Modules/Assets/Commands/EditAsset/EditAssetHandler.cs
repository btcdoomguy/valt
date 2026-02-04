using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.EditAsset;

internal sealed class EditAssetHandler : ICommandHandler<EditAssetCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<EditAssetCommand> _validator;

    public EditAssetHandler(
        IAssetRepository assetRepository,
        IValidator<EditAssetCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(
        EditAssetCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.NotFound("Asset", command.AssetId);

        // Validate currency
        try
        {
            FiatCurrency.GetFromCode(command.Details.CurrencyCode);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<Unit>.Failure(
                "INVALID_CURRENCY",
                $"Invalid currency code: {command.Details.CurrencyCode}");
        }

        var detailsResult = BuildDetails(command.Details);
        if (detailsResult.IsFailure)
            return Result<Unit>.Failure(detailsResult.Error!);

        var assetName = new AssetName(command.Name);
        var icon = string.IsNullOrWhiteSpace(command.Icon)
            ? asset.Icon
            : Icon.RestoreFromId(command.Icon);

        asset.Edit(assetName, detailsResult.Value!, icon, command.IncludeInNetWorth, command.Visible);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }

    private static Result<IAssetDetails> BuildDetails(AssetDetailsInputDTO dto)
    {
        return dto switch
        {
            BasicAssetDetailsInputDTO basic => Result<IAssetDetails>.Success(new BasicAssetDetails(
                assetType: (AssetTypes)basic.AssetType,
                quantity: basic.Quantity,
                symbol: basic.Symbol,
                priceSource: (AssetPriceSource)basic.PriceSource,
                currentPrice: basic.CurrentPrice,
                currencyCode: basic.CurrencyCode,
                acquisitionDate: basic.AcquisitionDate,
                acquisitionPrice: basic.AcquisitionPrice)),

            RealEstateAssetDetailsInputDTO realEstate => Result<IAssetDetails>.Success(new RealEstateAssetDetails(
                currentValue: realEstate.CurrentValue,
                currencyCode: realEstate.CurrencyCode,
                address: realEstate.Address,
                monthlyRentalIncome: realEstate.MonthlyRentalIncome,
                acquisitionDate: realEstate.AcquisitionDate,
                acquisitionPrice: realEstate.AcquisitionPrice)),

            LeveragedPositionDetailsInputDTO leveraged => Result<IAssetDetails>.Success(new LeveragedPositionDetails(
                collateral: leveraged.Collateral,
                entryPrice: leveraged.EntryPrice,
                leverage: leveraged.Leverage,
                liquidationPrice: leveraged.LiquidationPrice,
                currentPrice: leveraged.CurrentPrice,
                currencyCode: leveraged.CurrencyCode,
                symbol: leveraged.Symbol,
                priceSource: (AssetPriceSource)leveraged.PriceSource,
                isLong: leveraged.IsLong)),

            _ => Result<IAssetDetails>.Failure("UNKNOWN_DETAILS_TYPE", "Unknown asset details type")
        };
    }
}
