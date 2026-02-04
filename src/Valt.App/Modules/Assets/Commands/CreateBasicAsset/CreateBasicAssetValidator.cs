using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;

namespace Valt.App.Modules.Assets.Commands.CreateBasicAsset;

internal sealed class CreateBasicAssetValidator : IValidator<CreateBasicAssetCommand>
{
    private const int MaxNameLength = 100;

    public ValidationResult Validate(CreateBasicAssetCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.Name, nameof(instance.Name), "Asset name is required.");

        if (instance.Name?.Length > MaxNameLength)
            builder.AddError(nameof(instance.Name), $"Asset name cannot exceed {MaxNameLength} characters.");

        // Validate asset type is a valid basic asset type
        var validBasicTypes = new[] { (int)AssetTypes.Stock, (int)AssetTypes.Etf, (int)AssetTypes.Crypto, (int)AssetTypes.Commodity, (int)AssetTypes.Custom };
        if (!validBasicTypes.Contains(instance.AssetType))
            builder.AddError(nameof(instance.AssetType), "Asset type must be Stock (0), Etf (1), Crypto (2), Commodity (3), or Custom (6).");

        builder.AddErrorIfNullOrWhiteSpace(instance.CurrencyCode, nameof(instance.CurrencyCode), "Currency code is required.");

        if (instance.Quantity < 0)
            builder.AddError(nameof(instance.Quantity), "Quantity cannot be negative.");

        if (instance.CurrentPrice < 0)
            builder.AddError(nameof(instance.CurrentPrice), "Current price cannot be negative.");

        // Validate price source
        var validPriceSources = new[] { (int)AssetPriceSource.Manual, (int)AssetPriceSource.YahooFinance };
        if (!validPriceSources.Contains(instance.PriceSource))
            builder.AddError(nameof(instance.PriceSource), "Price source must be Manual (0) or YahooFinance (1).");

        return builder.Build();
    }
}
