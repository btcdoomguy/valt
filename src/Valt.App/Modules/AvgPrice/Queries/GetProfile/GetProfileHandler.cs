using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.Contracts;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Core.Modules.AvgPrice;

namespace Valt.App.Modules.AvgPrice.Queries.GetProfile;

internal sealed class GetProfileHandler : IQueryHandler<GetProfileQuery, AvgPriceProfileDTO?>
{
    private readonly IAvgPriceQueries _avgPriceQueries;

    public GetProfileHandler(IAvgPriceQueries avgPriceQueries)
    {
        _avgPriceQueries = avgPriceQueries;
    }

    public Task<AvgPriceProfileDTO?> HandleAsync(GetProfileQuery query, CancellationToken ct = default)
    {
        return _avgPriceQueries.GetProfileAsync(new AvgPriceProfileId(query.ProfileId));
    }
}
