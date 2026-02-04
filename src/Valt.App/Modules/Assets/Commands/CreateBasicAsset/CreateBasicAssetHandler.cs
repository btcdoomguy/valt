using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.CreateBasicAsset;

internal sealed class CreateBasicAssetHandler : ICommandHandler<CreateBasicAssetCommand, CreateBasicAssetResult>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<CreateBasicAssetCommand> _validator;

    public CreateBasicAssetHandler(
        IAssetRepository assetRepository,
        IValidator<CreateBasicAssetCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<CreateBasicAssetResult>> HandleAsync(
        CreateBasicAssetCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateBasicAssetResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Validate currency
        try
        {
            FiatCurrency.GetFromCode(command.CurrencyCode);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<CreateBasicAssetResult>.Failure(
                "INVALID_CURRENCY",
                $"Invalid currency code: {command.CurrencyCode}");
        }

        var assetName = new AssetName(command.Name);
        var assetType = (AssetTypes)command.AssetType;
        var priceSource = (AssetPriceSource)command.PriceSource;

        var details = new BasicAssetDetails(
            assetType: assetType,
            quantity: command.Quantity,
            symbol: command.Symbol,
            priceSource: priceSource,
            currentPrice: command.CurrentPrice,
            currencyCode: command.CurrencyCode);

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

        return Result<CreateBasicAssetResult>.Success(
            new CreateBasicAssetResult(asset.Id.Value));
    }
}
