using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.AvgPrice.Calculations;

public interface IAvgPriceRepository : IRepository
{
    Task<AvgPriceProfile?> GetAvgPriceProfileByIdAsync(AvgPriceProfileId avgPriceProfileId);
    Task SaveAvgPriceProfileAsync(AvgPriceProfile avgPriceProfile);
    Task<IEnumerable<AvgPriceProfile>> GetAvgPriceProfilesAsync();
    Task DeleteAvgPriceProfileAsync(AvgPriceProfile avgPriceProfile);
}