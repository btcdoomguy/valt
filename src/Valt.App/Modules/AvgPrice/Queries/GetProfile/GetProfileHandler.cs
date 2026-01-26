using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries;

namespace Valt.App.Modules.AvgPrice.Queries.GetProfile;

internal sealed class GetProfileHandler : IQueryHandler<GetProfileQuery, AvgPriceProfileDTO?>
{
    private readonly IAvgPriceQueries _avgPriceQueries;

    public GetProfileHandler(IAvgPriceQueries avgPriceQueries)
    {
        _avgPriceQueries = avgPriceQueries;
    }

    public async Task<AvgPriceProfileDTO?> HandleAsync(GetProfileQuery query, CancellationToken ct = default)
    {
        try
        {
            var infraResult = await _avgPriceQueries.GetProfileAsync(new AvgPriceProfileId(query.ProfileId));

            return new AvgPriceProfileDTO(
                infraResult.Id,
                infraResult.Name,
                infraResult.AssetName,
                infraResult.Precision,
                infraResult.Visible,
                infraResult.Icon,
                infraResult.Unicode,
                infraResult.Color.ToArgb(),
                infraResult.CurrencyCode,
                infraResult.AvgPriceCalculationMethodId
            );
        }
        catch
        {
            return null;
        }
    }
}
