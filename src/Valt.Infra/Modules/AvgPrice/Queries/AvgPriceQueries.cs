using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;

namespace Valt.Infra.Modules.AvgPrice.Queries;

internal sealed class AvgPriceQueries : IAvgPriceQueries
{
    private readonly ILocalDatabase _localDatabase;

    public AvgPriceQueries(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<IEnumerable<AvgPriceProfileListDTO>> GetProfilesAsync(bool showHidden = false)
    {
        var query = _localDatabase.GetAvgPriceProfiles().FindAll();

        if (!showHidden)
            query = query.Where(x => x.Visible);

        return Task.FromResult(query.Select(x =>
        {
            var icon = x.Icon != null ? Icon.RestoreFromId(x.Icon) : Icon.Empty;

            return new AvgPriceProfileListDTO(x.Id.ToString(), x.Name, x.AssetName, x.Visible, icon.Name, icon.Unicode, icon.Color,
                x.Currency, x.AvgPriceCalculationMethodId);
        }));
    }

    public Task<IEnumerable<AvgPriceLineDTO>> GetLinesOfProfileAsync(AvgPriceProfileId avgPriceProfileId)
    {
        var lines = _localDatabase.GetAvgPriceLines()
            .Find(x => x.ProfileId == new ObjectId(avgPriceProfileId.ToString()));

        return Task.FromResult(lines.Select(x => new AvgPriceLineDTO(x.Id.ToString(), DateOnly.FromDateTime(x.Date),
            x.DisplayOrder,
            x.AvgPriceLineTypeId,
            x.Quantity,
            x.UnitPrice,
            x.Comment,
            x.AvgCostOfAcquisition,
            x.TotalCost,
            x.TotalQuantity)));
    }
}