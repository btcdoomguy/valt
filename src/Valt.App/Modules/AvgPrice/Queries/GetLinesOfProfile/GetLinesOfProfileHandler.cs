using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.Contracts;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Core.Modules.AvgPrice;

namespace Valt.App.Modules.AvgPrice.Queries.GetLinesOfProfile;

internal sealed class GetLinesOfProfileHandler : IQueryHandler<GetLinesOfProfileQuery, IReadOnlyList<AvgPriceLineDTO>>
{
    private readonly IAvgPriceQueries _avgPriceQueries;

    public GetLinesOfProfileHandler(IAvgPriceQueries avgPriceQueries)
    {
        _avgPriceQueries = avgPriceQueries;
    }

    public async Task<IReadOnlyList<AvgPriceLineDTO>> HandleAsync(GetLinesOfProfileQuery query, CancellationToken ct = default)
    {
        var profileId = new AvgPriceProfileId(query.ProfileId);
        var result = await _avgPriceQueries.GetLinesOfProfileAsync(profileId);
        return result.ToList();
    }
}
