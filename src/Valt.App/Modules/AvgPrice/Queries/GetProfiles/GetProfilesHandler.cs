using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Infra.Modules.AvgPrice.Queries;

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
        var infraResult = await _avgPriceQueries.GetProfilesAsync(query.ShowHidden);

        return infraResult.Select(p => new AvgPriceProfileDTO(
            p.Id,
            p.Name,
            p.AssetName,
            p.Precision,
            p.Visible,
            p.Icon,
            p.Unicode,
            p.Color.ToArgb(),
            p.CurrencyCode,
            p.AvgPriceCalculationMethodId
        )).ToList();
    }
}
