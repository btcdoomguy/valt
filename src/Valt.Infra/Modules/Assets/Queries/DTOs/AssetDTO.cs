namespace Valt.Infra.Modules.Assets.Queries.DTOs;

public class AssetDTO
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AssetTypeId { get; set; }
    public string AssetTypeName { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public bool IncludeInNetWorth { get; set; }
    public bool Visible { get; set; }
    public DateTime LastPriceUpdateAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DisplayOrder { get; set; }

    // Price and value information
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public string CurrencyCode { get; set; } = null!;

    // Basic asset specific
    public decimal? Quantity { get; set; }
    public string? Symbol { get; set; }
    public int? PriceSourceId { get; set; }

    // Real estate specific
    public string? Address { get; set; }
    public decimal? MonthlyRentalIncome { get; set; }

    // Leveraged position specific
    public decimal? Collateral { get; set; }
    public decimal? EntryPrice { get; set; }
    public decimal? Leverage { get; set; }
    public decimal? LiquidationPrice { get; set; }
    public bool? IsLong { get; set; }
    public decimal? PnL { get; set; }
    public decimal? PnLPercentage { get; set; }
    public decimal? DistanceToLiquidation { get; set; }
    public bool? IsAtRisk { get; set; }
}
