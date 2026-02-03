using LiteDB;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Events;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Assets;

internal sealed class AssetRepository : IAssetRepository
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public AssetRepository(ILocalDatabase localDatabase, IDomainEventPublisher domainEventPublisher)
    {
        _localDatabase = localDatabase;
        _domainEventPublisher = domainEventPublisher;
    }

    public Task<Asset?> GetByIdAsync(AssetId id)
    {
        var entity = _localDatabase.GetAssets().FindById(new ObjectId(id.ToString()));
        return Task.FromResult(entity?.AsDomainObject());
    }

    public async Task SaveAsync(Asset asset)
    {
        var entity = asset.AsEntity();
        _localDatabase.GetAssets().Upsert(entity);

        foreach (var @event in asset.Events)
        {
            await _domainEventPublisher.PublishAsync(@event);
        }

        asset.ClearEvents();
    }

    public Task<IEnumerable<Asset>> GetAllAsync()
    {
        var entities = _localDatabase.GetAssets().FindAll();
        return Task.FromResult(entities.Select(e => e.AsDomainObject()));
    }

    public Task<IEnumerable<Asset>> GetVisibleAsync()
    {
        var entities = _localDatabase.GetAssets().Find(x => x.Visible);
        return Task.FromResult(entities.Select(e => e.AsDomainObject()));
    }

    public async Task DeleteAsync(Asset asset)
    {
        _localDatabase.GetAssets().Delete(new ObjectId(asset.Id.ToString()));

        foreach (var @event in asset.Events)
        {
            await _domainEventPublisher.PublishAsync(@event);
        }

        await _domainEventPublisher.PublishAsync(new AssetDeletedEvent(asset));

        asset.ClearEvents();
    }
}
