using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAssetGroups;

internal sealed class GetAssetGroupsHandler : IQueryHandler<GetAssetGroupsQuery, IReadOnlyList<AssetGroupDTO>>
{
    private readonly IAssetQueries _assetQueries;

    public GetAssetGroupsHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public Task<IReadOnlyList<AssetGroupDTO>> HandleAsync(GetAssetGroupsQuery query, CancellationToken ct = default)
    {
        return _assetQueries.GetAssetGroupsAsync();
    }
}
