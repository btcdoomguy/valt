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
    /// Gets asset summary with totals for net worth calculation.
    /// </summary>
    /// <param name="mainCurrencyCode">The main currency to convert totals to.</param>
    /// <param name="btcPriceUsd">Current BTC price in USD for sat conversion.</param>
    /// <param name="fiatRates">Fiat exchange rates relative to USD.</param>
    Task<AssetSummaryDTO> GetSummaryAsync(
        string mainCurrencyCode,
        decimal? btcPriceUsd = null,
        IReadOnlyDictionary<string, decimal>? fiatRates = null);
}
