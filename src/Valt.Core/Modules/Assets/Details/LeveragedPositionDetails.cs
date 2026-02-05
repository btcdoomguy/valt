namespace Valt.Core.Modules.Assets.Details;

/// <summary>
/// Asset details for leveraged positions (e.g., futures, margin trading).
/// </summary>
public sealed class LeveragedPositionDetails : IAssetDetails
{
    public AssetTypes AssetType => AssetTypes.LeveragedPosition;

    /// <summary>
    /// The collateral amount (initial margin).
    /// </summary>
    public decimal Collateral { get; }

    /// <summary>
    /// The entry price when the position was opened.
    /// </summary>
    public decimal EntryPrice { get; }

    /// <summary>
    /// The leverage multiplier (e.g., 2x, 5x, 10x).
    /// </summary>
    public decimal Leverage { get; }

    /// <summary>
    /// The liquidation price where the position would be closed.
    /// </summary>
    public decimal LiquidationPrice { get; }

    /// <summary>
    /// The current price of the underlying asset.
    /// </summary>
    public decimal CurrentPrice { get; }

    /// <summary>
    /// The currency code for the values (e.g., "USD", "BRL").
    /// </summary>
    public string CurrencyCode { get; }

    /// <summary>
    /// The ticker symbol for the underlying asset.
    /// </summary>
    public string? Symbol { get; }

    /// <summary>
    /// The price source for automatic price updates.
    /// </summary>
    public AssetPriceSource PriceSource { get; }

    /// <summary>
    /// True if this is a long position, false if short.
    /// </summary>
    public bool IsLong { get; }

    public LeveragedPositionDetails(
        decimal collateral,
        decimal entryPrice,
        decimal leverage,
        decimal liquidationPrice,
        decimal currentPrice,
        string currencyCode,
        string? symbol = null,
        AssetPriceSource priceSource = AssetPriceSource.Manual,
        bool isLong = true)
    {
        if (collateral <= 0)
            throw new ArgumentException("Collateral must be positive", nameof(collateral));

        if (entryPrice <= 0)
            throw new ArgumentException("Entry price must be positive", nameof(entryPrice));

        if (leverage < 1)
            throw new ArgumentException("Leverage must be at least 1", nameof(leverage));

        if (liquidationPrice < 0)
            throw new ArgumentException("Liquidation price cannot be negative", nameof(liquidationPrice));

        Collateral = collateral;
        EntryPrice = entryPrice;
        Leverage = leverage;
        LiquidationPrice = liquidationPrice;
        CurrentPrice = currentPrice;
        CurrencyCode = currencyCode;
        Symbol = symbol;
        PriceSource = priceSource;
        IsLong = isLong;
    }

    /// <summary>
    /// Calculates the current value of the leveraged position.
    /// For long positions: Collateral * (1 + PriceChange * Leverage)
    /// For short positions: Collateral * (1 - PriceChange * Leverage)
    /// </summary>
    public decimal CalculateCurrentValue(decimal currentPrice)
    {
        if (EntryPrice == 0)
            return Collateral;

        var priceChange = (currentPrice - EntryPrice) / EntryPrice;
        var leveragedChange = priceChange * Leverage;

        if (IsLong)
            return Collateral * (1 + leveragedChange);
        else
            return Collateral * (1 - leveragedChange);
    }

    /// <summary>
    /// Calculates the unrealized P&L.
    /// </summary>
    public decimal CalculatePnL(decimal currentPrice)
    {
        return CalculateCurrentValue(currentPrice) - Collateral;
    }

    /// <summary>
    /// Calculates the P&L percentage.
    /// </summary>
    public decimal CalculatePnLPercentage(decimal currentPrice)
    {
        if (Collateral == 0)
            return 0;

        return Math.Round(CalculatePnL(currentPrice) / Collateral * 100, 2);
    }

    /// <summary>
    /// Calculates the distance to liquidation as a percentage.
    /// </summary>
    public decimal CalculateDistanceToLiquidation(decimal currentPrice)
    {
        if (currentPrice == 0 || LiquidationPrice == 0)
            return 100;

        var distance = IsLong
            ? (currentPrice - LiquidationPrice) / currentPrice * 100
            : (LiquidationPrice - currentPrice) / currentPrice * 100;

        return Math.Round(Math.Max(0, distance), 2);
    }

    /// <summary>
    /// Determines if the position is at risk (within 10% of liquidation).
    /// </summary>
    public bool IsAtRisk(decimal currentPrice)
    {
        return CalculateDistanceToLiquidation(currentPrice) < 10;
    }

    public IAssetDetails WithUpdatedPrice(decimal newPrice)
    {
        return new LeveragedPositionDetails(
            Collateral,
            EntryPrice,
            Leverage,
            LiquidationPrice,
            newPrice,
            CurrencyCode,
            Symbol,
            PriceSource,
            IsLong);
    }

    public LeveragedPositionDetails WithCollateral(decimal newCollateral)
    {
        return new LeveragedPositionDetails(
            newCollateral,
            EntryPrice,
            Leverage,
            LiquidationPrice,
            CurrentPrice,
            CurrencyCode,
            Symbol,
            PriceSource,
            IsLong);
    }
}
