using Valt.Infra.Modules.Assets.Queries.DTOs;

namespace Valt.Infra.Modules.Assets.Queries;

public interface IAssetQueries
{
    Task<IReadOnlyList<AssetDTO>> GetAllAsync();
    Task<IReadOnlyList<AssetDTO>> GetVisibleAsync();
    Task<AssetDTO?> GetByIdAsync(string id);
    Task<AssetSummaryDTO> GetSummaryAsync(string mainCurrencyCode, decimal? btcPriceUsd = null, IReadOnlyDictionary<string, decimal>? fiatRates = null);
}
