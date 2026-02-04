using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;

internal sealed class CreateRealEstateAssetHandler : ICommandHandler<CreateRealEstateAssetCommand, CreateRealEstateAssetResult>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IValidator<CreateRealEstateAssetCommand> _validator;

    public CreateRealEstateAssetHandler(
        IAssetRepository assetRepository,
        IValidator<CreateRealEstateAssetCommand> validator)
    {
        _assetRepository = assetRepository;
        _validator = validator;
    }

    public async Task<Result<CreateRealEstateAssetResult>> HandleAsync(
        CreateRealEstateAssetCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateRealEstateAssetResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Validate currency
        try
        {
            FiatCurrency.GetFromCode(command.CurrencyCode);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<CreateRealEstateAssetResult>.Failure(
                "INVALID_CURRENCY",
                $"Invalid currency code: {command.CurrencyCode}");
        }

        var assetName = new AssetName(command.Name);

        var details = new RealEstateAssetDetails(
            address: command.Address,
            currentValue: command.CurrentValue,
            currencyCode: command.CurrencyCode,
            monthlyRentalIncome: command.MonthlyRentalIncome);

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

        return Result<CreateRealEstateAssetResult>.Success(
            new CreateRealEstateAssetResult(asset.Id.Value));
    }
}
