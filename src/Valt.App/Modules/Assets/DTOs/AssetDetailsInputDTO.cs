namespace Valt.App.Modules.Assets.DTOs;

/// <summary>
/// Base class for asset details input DTOs.
/// </summary>
public abstract record AssetDetailsInputDTO
{
    /// <summary>
    /// Currency code for the asset (e.g., USD, BRL).
    /// </summary>
    public required string CurrencyCode { get; init; }
}

/// <summary>
/// Input DTO for basic assets (Stock, ETF, Crypto, Commodity, Custom).
/// </summary>
public record BasicAssetDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Asset type: 0=Stock, 1=Etf, 2=Crypto, 3=Commodity, 6=Custom
    /// </summary>
    public required int AssetType { get; init; }

    /// <summary>
    /// Quantity held.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Ticker symbol (e.g., AAPL, BTC).
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Price source: 0=Manual, 1=YahooFinance
    /// </summary>
    public required int PriceSource { get; init; }

    /// <summary>
    /// Current price per unit.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

    /// <summary>
    /// Acquisition date (optional).
    /// </summary>
    public DateOnly? AcquisitionDate { get; init; }

    /// <summary>
    /// Price per unit at acquisition (optional).
    /// </summary>
    public decimal? AcquisitionPrice { get; init; }
}

/// <summary>
/// Input DTO for real estate assets.
/// </summary>
public record RealEstateAssetDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Property address (optional).
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Current market value.
    /// </summary>
    public required decimal CurrentValue { get; init; }

    /// <summary>
    /// Monthly rental income (optional).
    /// </summary>
    public decimal? MonthlyRentalIncome { get; init; }

    /// <summary>
    /// Acquisition date (optional).
    /// </summary>
    public DateOnly? AcquisitionDate { get; init; }

    /// <summary>
    /// Total purchase price at acquisition (optional).
    /// </summary>
    public decimal? AcquisitionPrice { get; init; }
}

/// <summary>
/// Input DTO for leveraged position assets.
/// </summary>
public record LeveragedPositionDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Ticker symbol (e.g., BTC-PERP).
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Collateral amount.
    /// </summary>
    public required decimal Collateral { get; init; }

    /// <summary>
    /// Entry price.
    /// </summary>
    public required decimal EntryPrice { get; init; }

    /// <summary>
    /// Current price.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

    /// <summary>
    /// Leverage multiplier (e.g., 2, 5, 10).
    /// </summary>
    public required decimal Leverage { get; init; }

    /// <summary>
    /// Liquidation price.
    /// </summary>
    public required decimal LiquidationPrice { get; init; }

    /// <summary>
    /// True for long position, false for short.
    /// </summary>
    public required bool IsLong { get; init; }

    /// <summary>
    /// Price source: 0=Manual, 1=YahooFinance
    /// </summary>
    public required int PriceSource { get; init; }
}
