using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetLatestLoanState;

internal sealed class GetLatestLoanStateHandler : IQueryHandler<GetLatestLoanStateQuery, LoanStateDTO?>
{
    private readonly IAssetQueries _assetQueries;

    public GetLatestLoanStateHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public async Task<LoanStateDTO?> HandleAsync(GetLatestLoanStateQuery query, CancellationToken ct = default)
    {
        return await _assetQueries.GetLatestLoanStateAsync(query.AssetId);
    }
}
