using Valt.App.Kernel.Validation;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Modules.Assets;

namespace Valt.App.Modules.Assets.Commands.EditAsset;

internal sealed class EditAssetValidator : IValidator<EditAssetCommand>
{
    private const int MaxNameLength = 100;
    private const int MaxAddressLength = 500;

    public ValidationResult Validate(EditAssetCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");
        builder.AddErrorIfNullOrWhiteSpace(instance.Name, nameof(instance.Name), "Asset name is required.");

        if (instance.Name?.Length > MaxNameLength)
            builder.AddError(nameof(instance.Name), $"Asset name cannot exceed {MaxNameLength} characters.");

        if (instance.Details is null)
        {
            builder.AddError(nameof(instance.Details), "Asset details are required.");
            return builder.Build();
        }

        builder.AddErrorIfNullOrWhiteSpace(instance.Details.CurrencyCode, "Details.CurrencyCode", "Currency code is required.");

        switch (instance.Details)
        {
            case BasicAssetDetailsInputDTO basic:
                ValidateBasicDetails(basic, builder);
                break;

            case RealEstateAssetDetailsInputDTO realEstate:
                ValidateRealEstateDetails(realEstate, builder);
                break;

            case LeveragedPositionDetailsInputDTO leveraged:
                ValidateLeveragedDetails(leveraged, builder);
                break;

            default:
                builder.AddError(nameof(instance.Details), "Unknown asset details type.");
                break;
        }

        return builder.Build();
    }

    private static void ValidateBasicDetails(BasicAssetDetailsInputDTO details, ValidationResultBuilder builder)
    {
        var validBasicTypes = new[] { (int)AssetTypes.Stock, (int)AssetTypes.Etf, (int)AssetTypes.Crypto, (int)AssetTypes.Commodity, (int)AssetTypes.Custom };
        if (!validBasicTypes.Contains(details.AssetType))
            builder.AddError("Details.AssetType", "Asset type must be Stock (0), Etf (1), Crypto (2), Commodity (3), or Custom (6).");

        if (details.Quantity < 0)
            builder.AddError("Details.Quantity", "Quantity cannot be negative.");

        if (details.CurrentPrice < 0)
            builder.AddError("Details.CurrentPrice", "Current price cannot be negative.");

        var validPriceSources = new[] { (int)AssetPriceSource.Manual, (int)AssetPriceSource.YahooFinance };
        if (!validPriceSources.Contains(details.PriceSource))
            builder.AddError("Details.PriceSource", "Price source must be Manual (0) or YahooFinance (1).");
    }

    private void ValidateRealEstateDetails(RealEstateAssetDetailsInputDTO details, ValidationResultBuilder builder)
    {
        if (details.CurrentValue < 0)
            builder.AddError("Details.CurrentValue", "Current value cannot be negative.");

        if (details.Address?.Length > MaxAddressLength)
            builder.AddError("Details.Address", $"Address cannot exceed {MaxAddressLength} characters.");

        if (details.MonthlyRentalIncome.HasValue && details.MonthlyRentalIncome.Value < 0)
            builder.AddError("Details.MonthlyRentalIncome", "Monthly rental income cannot be negative.");
    }

    private static void ValidateLeveragedDetails(LeveragedPositionDetailsInputDTO details, ValidationResultBuilder builder)
    {
        builder.AddErrorIfNullOrWhiteSpace(details.Symbol, "Details.Symbol", "Symbol is required.");

        if (details.Collateral <= 0)
            builder.AddError("Details.Collateral", "Collateral must be greater than zero.");

        if (details.EntryPrice <= 0)
            builder.AddError("Details.EntryPrice", "Entry price must be greater than zero.");

        if (details.CurrentPrice <= 0)
            builder.AddError("Details.CurrentPrice", "Current price must be greater than zero.");

        if (details.Leverage < 1)
            builder.AddError("Details.Leverage", "Leverage must be at least 1.");

        if (details.LiquidationPrice <= 0)
            builder.AddError("Details.LiquidationPrice", "Liquidation price must be greater than zero.");

        var validPriceSources = new[] { (int)AssetPriceSource.Manual, (int)AssetPriceSource.YahooFinance, (int)AssetPriceSource.LivePrice };
        if (!validPriceSources.Contains(details.PriceSource))
            builder.AddError("Details.PriceSource", "Price source must be Manual (0), YahooFinance (1), or LivePrice (2).");
    }
}
