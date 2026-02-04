using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAsset;

internal sealed class GetAssetHandler : IQueryHandler<GetAssetQuery, AssetDTO?>
{
    private readonly IAssetQueries _assetQueries;

    public GetAssetHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public Task<AssetDTO?> HandleAsync(GetAssetQuery query, CancellationToken ct = default)
    {
        return _assetQueries.GetByIdAsync(query.AssetId);
    }
}
