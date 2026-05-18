using LiteDB;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Assets;

internal class AssetGroupRepository : IAssetGroupRepository
{
    private readonly ILocalDatabase _localDatabase;

    public AssetGroupRepository(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<AssetGroup?> GetByIdAsync(AssetGroupId id)
    {
        var entity = _localDatabase.GetAssetGroups().FindById(new ObjectId(id));
        return Task.FromResult(entity?.AsDomainObject());
    }

    public Task<IList<AssetGroup>> GetAllAsync()
    {
        var entities = _localDatabase.GetAssetGroups().FindAll();
        var groups = entities.Select(e => e.AsDomainObject()).OrderBy(g => g.DisplayOrder).ToList();
        return Task.FromResult<IList<AssetGroup>>(groups);
    }

    public Task SaveAsync(AssetGroup group)
    {
        var entity = group.AsEntity();
        _localDatabase.GetAssetGroups().Upsert(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AssetGroupId id)
    {
        var groupIdBson = new ObjectId(id.Value);

        // Unlink all assets from this group
        var assets = _localDatabase.GetAssets().Find(a => a.GroupId == groupIdBson).ToList();
        foreach (var asset in assets)
        {
            asset.GroupId = null;
            _localDatabase.GetAssets().Update(asset);
        }

        _localDatabase.GetAssetGroups().Delete(groupIdBson);
        return Task.CompletedTask;
    }
}
