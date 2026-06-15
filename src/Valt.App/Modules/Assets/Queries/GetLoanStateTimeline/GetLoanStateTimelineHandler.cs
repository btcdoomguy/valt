using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetLoanStateTimeline;

internal sealed class GetLoanStateTimelineHandler : IQueryHandler<GetLoanStateTimelineQuery, IReadOnlyList<LoanStateSnapshotDTO>>
{
    private readonly IAssetQueries _assetQueries;

    public GetLoanStateTimelineHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public async Task<IReadOnlyList<LoanStateSnapshotDTO>> HandleAsync(GetLoanStateTimelineQuery query, CancellationToken ct = default)
    {
        return await _assetQueries.GetLoanStateTimelineAsync(query.AssetId);
    }
}
