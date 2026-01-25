using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries;

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
        var infraResult = await _avgPriceQueries.GetLinesOfProfileAsync(profileId);

        return infraResult.Select(l => new AvgPriceLineDTO(
            l.Id,
            l.Date,
            l.DisplayOrder,
            l.AvgPriceLineTypeId,
            l.Quantity,
            l.Amount,
            l.Comment,
            l.AvgCostOfAcquisition,
            l.TotalCost,
            l.TotalQuantity
        )).ToList();
    }
}
