using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Crawlers.Indicators;

internal class IndicatorCache : IIndicatorCache
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly IClock _clock;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    public IndicatorCache(IPriceDatabase priceDatabase, IClock clock)
    {
        _priceDatabase = priceDatabase;
        _clock = clock;
    }

    public IndicatorSnapshot? GetLatest()
    {
        if (!_priceDatabase.HasDatabaseOpen)
            return null;

        var collection = _priceDatabase.GetIndicators();
        var entity = collection.FindById("latest");
        if (entity is null)
            return null;

        var isUpToDate = (_clock.GetCurrentDateTimeUtc() - entity.LastUpdatedUtc) < StaleThreshold;
        return entity.ToSnapshot(isUpToDate);
    }

    public void Save(IndicatorSnapshot snapshot)
    {
        if (!_priceDatabase.HasDatabaseOpen)
            return;

        var entity = IndicatorSnapshotEntity.FromSnapshot(snapshot);
        var collection = _priceDatabase.GetIndicators();
        collection.Upsert(entity);
        _priceDatabase.Checkpoint();
    }
}
