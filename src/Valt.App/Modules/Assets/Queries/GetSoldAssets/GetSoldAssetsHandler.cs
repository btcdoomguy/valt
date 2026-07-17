using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetSoldAssets;

internal sealed class GetSoldAssetsHandler : IQueryHandler<GetSoldAssetsQuery, IReadOnlyList<AssetDTO>>
{
    private readonly IAssetQueries _assetQueries;

    public GetSoldAssetsHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public Task<IReadOnlyList<AssetDTO>> HandleAsync(GetSoldAssetsQuery query, CancellationToken ct = default)
    {
        return _assetQueries.GetSoldAsync();
    }
}
