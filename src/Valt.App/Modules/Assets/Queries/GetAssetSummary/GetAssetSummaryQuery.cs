using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAssetSummary;

/// <summary>
/// Query to get asset summary with totals for net worth calculation.
/// </summary>
public record GetAssetSummaryQuery : IQuery<AssetSummaryDTO>
{
    /// <summary>
    /// The main currency to convert totals to.
    /// </summary>
    public required string MainCurrencyCode { get; init; }

    /// <summary>
    /// Current BTC price in USD for sat conversion (optional).
    /// </summary>
    public decimal? BtcPriceUsd { get; init; }

    /// <summary>
    /// Fiat exchange rates relative to USD (optional).
    /// </summary>
    public IReadOnlyDictionary<string, decimal>? FiatRates { get; init; }
}
