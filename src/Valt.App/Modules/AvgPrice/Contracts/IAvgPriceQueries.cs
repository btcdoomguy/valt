using Valt.Core.Modules.AvgPrice;
using Valt.App.Modules.AvgPrice.DTOs;

namespace Valt.App.Modules.AvgPrice.Contracts;

public interface IAvgPriceQueries
{
    Task<IEnumerable<AvgPriceProfileDTO>> GetProfilesAsync(bool showHidden = false);
    Task<AvgPriceProfileDTO?> GetProfileAsync(AvgPriceProfileId id);
    Task<IEnumerable<AvgPriceLineDTO>> GetLinesOfProfileAsync(AvgPriceProfileId id);
}
