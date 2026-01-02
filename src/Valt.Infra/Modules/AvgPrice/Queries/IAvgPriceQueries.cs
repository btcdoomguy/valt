using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;

namespace Valt.Infra.Modules.AvgPrice.Queries;

public interface IAvgPriceQueries
{
    Task<IEnumerable<AvgPriceProfileDTO>> GetProfilesAsync(bool showHidden = false);
    Task<AvgPriceProfileDTO> GetProfileAsync(AvgPriceProfileId id);
    Task<IEnumerable<AvgPriceLineDTO>> GetLinesOfProfileAsync(AvgPriceProfileId id);
}