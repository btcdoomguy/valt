using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Contracts;

/// <summary>
/// Query interface for reading asset data.
/// </summary>
public interface IAssetQueries
{
    /// <summary>
    /// Gets all assets, ordered by visibility (visible first), then display order, then name.
    /// </summary>
    Task<IReadOnlyList<AssetDTO>> GetAllAsync();

    /// <summary>
    /// Gets only visible assets, ordered by display order, then name.
    /// </summary>
    Task<IReadOnlyList<AssetDTO>> GetVisibleAsync();

    /// <summary>
    /// Gets a single asset by its ID.
    /// </summary>
    Task<AssetDTO?> GetByIdAsync(string id);

    /// <summary>
    /// Gets the full chronological snapshot timeline for a BTC-backed loan.
    /// </summary>
    Task<IReadOnlyList<LoanStateSnapshotDTO>> GetLoanStateTimelineAsync(string assetId);

    /// <summary>
    /// Gets the latest recorded state of a BTC-backed loan, including asset metadata.
    /// </summary>
    Task<LoanStateDTO?> GetLatestLoanStateAsync(string assetId);

    /// <summary>
    /// Gets asset summary with totals for net worth calculation.
    /// </summary>
    /// <param name="mainCurrencyCode">The main currency to convert totals to.</param>
    /// <param name="btcPriceUsd">Current BTC price in USD for sat conversion.</param>
    /// <param name="fiatRates">Fiat exchange rates relative to USD.</param>
    /// <param name="customBtcPriceUsd">Custom BTC price in USD for simulation. Overrides btcPriceUsd when provided.</param>
    Task<AssetSummaryDTO> GetSummaryAsync(
        string mainCurrencyCode,
        decimal? btcPriceUsd = null,
        IReadOnlyDictionary<string, decimal>? fiatRates = null,
        decimal? customBtcPriceUsd = null);

    /// <summary>
    /// Gets all asset groups ordered by display order.
    /// </summary>
    Task<IReadOnlyList<AssetGroupDTO>> GetAssetGroupsAsync();
}
