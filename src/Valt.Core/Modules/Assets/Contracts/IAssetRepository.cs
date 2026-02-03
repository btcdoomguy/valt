using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.Assets.Contracts;

public interface IAssetRepository : IRepository
{
    Task<Asset?> GetByIdAsync(AssetId id);
    Task SaveAsync(Asset asset);
    Task<IEnumerable<Asset>> GetAllAsync();
    Task<IEnumerable<Asset>> GetVisibleAsync();
    Task DeleteAsync(Asset asset);
}
