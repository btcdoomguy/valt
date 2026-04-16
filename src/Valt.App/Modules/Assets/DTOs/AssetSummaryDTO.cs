namespace Valt.App.Modules.Assets.DTOs;

/// <summary>
/// Summary of all assets for net worth calculations.
/// </summary>
public record AssetSummaryDTO
{
    public required int TotalAssets { get; init; }
    public required int VisibleAssets { get; init; }
    public required int AssetsIncludedInNetWorth { get; init; }

    /// <summary>
    /// Total value of all assets included in net worth, grouped by currency.
    /// </summary>
    public required IReadOnlyList<AssetValueByCurrencyDTO> ValuesByCurrency { get; init; }

    /// <summary>
    /// Total value of positive assets (non-liability) included in net worth, in main currency.
    /// </summary>
    public required decimal TotalAssetsValueInMainCurrency { get; init; }

    /// <summary>
    /// Total debt from liabilities (BTC loans) included in net worth, in main currency.
    /// Displayed as a positive number representing the debt amount.
    /// </summary>
    public required decimal TotalLiabilitiesInMainCurrency { get; init; }

    /// <summary>
    /// Number of liability assets (BTC loans) included in net worth.
    /// </summary>
    public required int LiabilitiesCount { get; init; }

    /// <summary>
    /// Net total value converted to the main fiat currency (assets - liabilities).
    /// </summary>
    public required decimal TotalValueInMainCurrency { get; init; }

    /// <summary>
    /// Total value converted to satoshis.
    /// </summary>
    public required long TotalValueInSats { get; init; }
}

/// <summary>
/// Asset value breakdown by currency.
/// </summary>
public record AssetValueByCurrencyDTO
{
    public required string CurrencyCode { get; init; }
    public required decimal TotalValue { get; init; }
    public required int AssetCount { get; init; }
}
