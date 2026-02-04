using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAssetSummary;

internal sealed class GetAssetSummaryHandler : IQueryHandler<GetAssetSummaryQuery, AssetSummaryDTO>
{
    private readonly IAssetQueries _assetQueries;

    public GetAssetSummaryHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public Task<AssetSummaryDTO> HandleAsync(GetAssetSummaryQuery query, CancellationToken ct = default)
    {
        return _assetQueries.GetSummaryAsync(
            query.MainCurrencyCode,
            query.BtcPriceUsd,
            query.FiatRates);
    }
}
