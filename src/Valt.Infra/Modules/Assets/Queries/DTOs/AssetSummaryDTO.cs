namespace Valt.Infra.Modules.Assets.Queries.DTOs;

public class AssetSummaryDTO
{
    public int TotalAssets { get; set; }
    public int VisibleAssets { get; set; }
    public int AssetsIncludedInNetWorth { get; set; }

    /// <summary>
    /// Total value of all assets included in net worth, grouped by currency.
    /// </summary>
    public List<AssetValueByCurrencyDTO> ValuesByCurrency { get; set; } = new();

    /// <summary>
    /// Total value converted to the main fiat currency.
    /// </summary>
    public decimal TotalValueInMainCurrency { get; set; }

    /// <summary>
    /// Total value converted to satoshis.
    /// </summary>
    public long TotalValueInSats { get; set; }
}

public class AssetValueByCurrencyDTO
{
    public string CurrencyCode { get; set; } = null!;
    public decimal TotalValue { get; set; }
    public int AssetCount { get; set; }
}
