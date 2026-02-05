using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetVisibleAssets;

internal sealed class GetVisibleAssetsHandler : IQueryHandler<GetVisibleAssetsQuery, IReadOnlyList<AssetDTO>>
{
    private readonly IAssetQueries _assetQueries;

    public GetVisibleAssetsHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public Task<IReadOnlyList<AssetDTO>> HandleAsync(GetVisibleAssetsQuery query, CancellationToken ct = default)
    {
        return _assetQueries.GetVisibleAsync();
    }
}
