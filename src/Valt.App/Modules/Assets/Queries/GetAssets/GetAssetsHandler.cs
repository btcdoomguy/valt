using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAssets;

internal sealed class GetAssetsHandler : IQueryHandler<GetAssetsQuery, IReadOnlyList<AssetDTO>>
{
    private readonly IAssetQueries _assetQueries;

    public GetAssetsHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public Task<IReadOnlyList<AssetDTO>> HandleAsync(GetAssetsQuery query, CancellationToken ct = default)
    {
        return _assetQueries.GetAllAsync();
    }
}
