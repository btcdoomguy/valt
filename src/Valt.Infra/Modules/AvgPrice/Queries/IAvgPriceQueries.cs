using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;

namespace Valt.Infra.Modules.AvgPrice.Queries;

public interface IAvgPriceQueries
{
    Task<IEnumerable<AvgPriceProfileListDTO>> GetProfilesAsync(bool showHidden = false);
    Task<IEnumerable<AvgPriceLineDTO>> GetLinesOfProfileAsync(AvgPriceProfileId avgPriceProfileId);
}