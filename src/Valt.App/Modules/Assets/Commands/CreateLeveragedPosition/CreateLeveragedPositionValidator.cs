using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;

namespace Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;

internal sealed class CreateLeveragedPositionValidator : IValidator<CreateLeveragedPositionCommand>
{
    private const int MaxNameLength = 100;

    public ValidationResult Validate(CreateLeveragedPositionCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.Name, nameof(instance.Name), "Position name is required.");

        if (instance.Name?.Length > MaxNameLength)
            builder.AddError(nameof(instance.Name), $"Position name cannot exceed {MaxNameLength} characters.");

        builder.AddErrorIfNullOrWhiteSpace(instance.CurrencyCode, nameof(instance.CurrencyCode), "Currency code is required.");

        builder.AddErrorIfNullOrWhiteSpace(instance.Symbol, nameof(instance.Symbol), "Symbol is required.");

        if (instance.Collateral <= 0)
            builder.AddError(nameof(instance.Collateral), "Collateral must be greater than zero.");

        if (instance.EntryPrice <= 0)
            builder.AddError(nameof(instance.EntryPrice), "Entry price must be greater than zero.");

        if (instance.CurrentPrice <= 0)
            builder.AddError(nameof(instance.CurrentPrice), "Current price must be greater than zero.");

        if (instance.Leverage < 1)
            builder.AddError(nameof(instance.Leverage), "Leverage must be at least 1.");

        if (instance.LiquidationPrice <= 0)
            builder.AddError(nameof(instance.LiquidationPrice), "Liquidation price must be greater than zero.");

        // Validate price source
        var validPriceSources = new[] { (int)AssetPriceSource.Manual, (int)AssetPriceSource.YahooFinance, (int)AssetPriceSource.LivePrice };
        if (!validPriceSources.Contains(instance.PriceSource))
            builder.AddError(nameof(instance.PriceSource), "Price source must be Manual (0), YahooFinance (1), or LivePrice (2).");

        return builder.Build();
    }
}
