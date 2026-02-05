namespace Valt.Core.Modules.Assets.Details;

/// <summary>
/// Basic asset details for stocks, ETFs, crypto, commodities, and custom assets.
/// </summary>
public sealed class BasicAssetDetails : IAssetDetails
{
    public AssetTypes AssetType { get; }

    /// <summary>
    /// The quantity of units held.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// The ticker symbol for price lookup (e.g., "AAPL", "bitcoin").
    /// </summary>
    public string? Symbol { get; }

    /// <summary>
    /// The price source for automatic price updates.
    /// </summary>
    public AssetPriceSource PriceSource { get; }

    /// <summary>
    /// The current price per unit.
    /// </summary>
    public decimal CurrentPrice { get; }

    /// <summary>
    /// The currency code for the price (e.g., "USD", "BRL").
    /// </summary>
    public string CurrencyCode { get; }

    /// <summary>
    /// The date when the asset was acquired (optional).
    /// </summary>
    public DateOnly? AcquisitionDate { get; }

    /// <summary>
    /// The price per unit at acquisition (optional).
    /// </summary>
    public decimal? AcquisitionPrice { get; }

    public BasicAssetDetails(
        AssetTypes assetType,
        decimal quantity,
        string? symbol,
        AssetPriceSource priceSource,
        decimal currentPrice,
        string currencyCode,
        DateOnly? acquisitionDate = null,
        decimal? acquisitionPrice = null)
    {
        if (assetType is AssetTypes.RealEstate or AssetTypes.LeveragedPosition)
            throw new ArgumentException($"Invalid asset type {assetType} for BasicAssetDetails", nameof(assetType));

        AssetType = assetType;
        Quantity = quantity;
        Symbol = symbol;
        PriceSource = priceSource;
        CurrentPrice = currentPrice;
        CurrencyCode = currencyCode;
        AcquisitionDate = acquisitionDate;
        AcquisitionPrice = acquisitionPrice;
    }

    public decimal CalculateCurrentValue(decimal currentPrice) => Quantity * currentPrice;

    /// <summary>
    /// Calculates the profit/loss based on acquisition price.
    /// </summary>
    public decimal CalculatePnL() => AcquisitionPrice.HasValue
        ? (CurrentPrice - AcquisitionPrice.Value) * Quantity
        : 0;

    /// <summary>
    /// Calculates the profit/loss percentage based on acquisition price.
    /// </summary>
    public decimal CalculatePnLPercentage() => AcquisitionPrice.HasValue && AcquisitionPrice.Value != 0
        ? Math.Round((CurrentPrice - AcquisitionPrice.Value) / AcquisitionPrice.Value * 100, 2)
        : 0;

    public IAssetDetails WithUpdatedPrice(decimal newPrice)
    {
        return new BasicAssetDetails(
            AssetType,
            Quantity,
            Symbol,
            PriceSource,
            newPrice,
            CurrencyCode,
            AcquisitionDate,
            AcquisitionPrice);
    }

    public BasicAssetDetails WithQuantity(decimal newQuantity)
    {
        return new BasicAssetDetails(
            AssetType,
            newQuantity,
            Symbol,
            PriceSource,
            CurrentPrice,
            CurrencyCode,
            AcquisitionDate,
            AcquisitionPrice);
    }
}
