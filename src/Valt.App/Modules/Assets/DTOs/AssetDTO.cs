namespace Valt.App.Modules.Assets.DTOs;

/// <summary>
/// DTO representing an Asset for display purposes.
/// </summary>
public record AssetDTO
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int AssetTypeId { get; init; }
    public required string AssetTypeName { get; init; }
    public required string Icon { get; init; }
    public required bool IncludeInNetWorth { get; init; }
    public required bool Visible { get; init; }
    public required DateTime LastPriceUpdateAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required int DisplayOrder { get; init; }

    // Price and value information
    public required decimal CurrentPrice { get; init; }
    public required decimal CurrentValue { get; init; }
    public required string CurrencyCode { get; init; }

    // Basic asset specific (nullable for other types)
    public decimal? Quantity { get; init; }
    public string? Symbol { get; init; }
    public int? PriceSourceId { get; init; }

    // Real estate specific
    public string? Address { get; init; }
    public decimal? MonthlyRentalIncome { get; init; }

    // Leveraged position specific
    public decimal? Collateral { get; init; }
    public decimal? EntryPrice { get; init; }
    public decimal? Leverage { get; init; }
    public decimal? LiquidationPrice { get; init; }
    public bool? IsLong { get; init; }
    public decimal? DistanceToLiquidation { get; init; }
    public bool? IsAtRisk { get; init; }

    // Common acquisition and P&L fields (for all asset types)
    public DateOnly? AcquisitionDate { get; init; }
    public decimal? AcquisitionPrice { get; init; }
    public decimal? PnL { get; init; }
    public decimal? PnLPercentage { get; init; }
}
