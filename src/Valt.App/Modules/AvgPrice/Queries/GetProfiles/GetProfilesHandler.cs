using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.Contracts;
using Valt.App.Modules.AvgPrice.DTOs;

namespace Valt.App.Modules.AvgPrice.Queries.GetProfiles;

internal sealed class GetProfilesHandler : IQueryHandler<GetProfilesQuery, IReadOnlyList<AvgPriceProfileDTO>>
{
    private readonly IAvgPriceQueries _avgPriceQueries;

    public GetProfilesHandler(IAvgPriceQueries avgPriceQueries)
    {
        _avgPriceQueries = avgPriceQueries;
    }

    public async Task<IReadOnlyList<AvgPriceProfileDTO>> HandleAsync(GetProfilesQuery query, CancellationToken ct = default)
    {
        var result = await _avgPriceQueries.GetProfilesAsync(query.ShowHidden);
        return result.ToList();
    }
}
