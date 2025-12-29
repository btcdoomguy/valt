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

    public Task<IEnumerable<AvgPriceProfileDTO>> GetProfilesAsync(bool showHidden = false)
    {
        var query = _localDatabase.GetAvgPriceProfiles().FindAll();

        if (!showHidden)
            query = query.Where(x => x.Visible);

        return Task.FromResult(query.Select(AsDto));
    }

    public Task<AvgPriceProfileDTO> GetProfileAsync(AvgPriceProfileId id)
    {
        var entity = _localDatabase.GetAvgPriceProfiles().FindById(new ObjectId(id.ToString()));

        return Task.FromResult(AsDto(entity));
    }

    private AvgPriceProfileDTO AsDto(AvgPriceProfileEntity entity)
    {
        var icon = entity.Icon != null ? Icon.RestoreFromId(entity.Icon) : Icon.Empty;

        return new AvgPriceProfileDTO(entity.Id.ToString(), entity.Name, entity.AssetName,
            entity.Precision, entity.Visible, icon.ToString(), icon.Unicode, icon.Color,
            entity.Currency, entity.AvgPriceCalculationMethodId);
    }

    public Task<IEnumerable<AvgPriceLineDTO>> GetLinesOfProfileAsync(AvgPriceProfileId id)
    {
        var lines = _localDatabase.GetAvgPriceLines()
            .Find(x => x.ProfileId == new ObjectId(id.ToString()));

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