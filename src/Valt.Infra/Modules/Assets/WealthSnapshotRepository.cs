using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Assets;

/// <summary>
/// Persistence for <see cref="WealthSnapshotEntity"/>. The database is the source of truth
/// for the one-row-per-day invariant — dedupe is decided by querying the collection for the
/// day's <see cref="WealthSnapshotEntity.SnapshotDay"/>, never by an in-memory flag.
/// </summary>
internal sealed class WealthSnapshotRepository : IWealthSnapshotRepository
{
    private readonly ILocalDatabase _localDatabase;

    public WealthSnapshotRepository(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<WealthSnapshotEntity?> GetByDayAsync(DateOnly day)
    {
        var entity = _localDatabase.GetWealthSnapshots().FindOne(x => x.SnapshotDay == day);
        return Task.FromResult(entity);
    }

    public Task SaveAsync(WealthSnapshotEntity entity)
    {
        var collection = _localDatabase.GetWealthSnapshots();
        var existing = collection.FindOne(x => x.SnapshotDay == entity.SnapshotDay);

        if (existing is not null)
        {
            entity.Id = existing.Id;
            collection.Update(entity);
        }
        else
        {
            collection.Insert(entity);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WealthSnapshotEntity>> GetAllAsync()
    {
        var entities = _localDatabase.GetWealthSnapshots().FindAll().ToList();
        return Task.FromResult<IReadOnlyList<WealthSnapshotEntity>>(entities);
    }
}
