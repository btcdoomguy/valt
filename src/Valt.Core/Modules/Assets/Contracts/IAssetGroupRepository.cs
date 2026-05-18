using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.Assets.Contracts;

public interface IAssetGroupRepository : IRepository
{
    Task<AssetGroup?> GetByIdAsync(AssetGroupId id);
    Task<IList<AssetGroup>> GetAllAsync();
    Task SaveAsync(AssetGroup group);
    Task DeleteAsync(AssetGroupId id);
}
