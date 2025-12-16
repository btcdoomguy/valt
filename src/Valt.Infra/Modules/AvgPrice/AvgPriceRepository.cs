using LiteDB;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Core.Modules.AvgPrice.Events;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.AvgPrice;

internal sealed class AvgPriceRepository : IAvgPriceRepository
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public AvgPriceRepository(ILocalDatabase localDatabase,
        IDomainEventPublisher domainEventPublisher)
    {
        _localDatabase = localDatabase;
        _domainEventPublisher = domainEventPublisher;
    }

    public Task<AvgPriceProfile?> GetAvgPriceProfileByIdAsync(AvgPriceProfileId avgPriceProfileId)
    {
        var entity = _localDatabase.GetAvgPriceProfiles().FindById(new ObjectId(avgPriceProfileId.ToString()));

        var lines = _localDatabase.GetAvgPriceLines().Find(x => x.ProfileId == new ObjectId(avgPriceProfileId.ToString()));

        return Task.FromResult(entity?.AsDomainObject(lines));
    }

    public async Task SaveAvgPriceProfileAsync(AvgPriceProfile avgPriceProfile)
    {
        var profileEntity = avgPriceProfile.AsEntity();

        foreach (var @event in avgPriceProfile.Events)
        {
            _localDatabase.GetAvgPriceProfiles().Upsert(profileEntity);
            if (@event.GetType() == typeof(AvgPriceLineCreatedEvent))
            {
                var lineEntity = (@event as AvgPriceLineCreatedEvent)!.AvgPriceLine.AsEntity(avgPriceProfile.Id);

                _localDatabase.GetAvgPriceLines().Upsert(lineEntity);
            }
            else if (@event.GetType() == typeof(AvgPriceLineUpdatedEvent))
            {
                var lineEntity = (@event as AvgPriceLineUpdatedEvent)!.AvgPriceLine.AsEntity(avgPriceProfile.Id);

                _localDatabase.GetAvgPriceLines().Upsert(lineEntity);
            }
            else if (@event.GetType() == typeof(AvgPriceLineDeletedEvent))
            {
                _localDatabase.GetAvgPriceLines().Delete(new ObjectId(avgPriceProfile.Id.ToString()));
            }

            await _domainEventPublisher.PublishAsync(@event);
        }
        
        avgPriceProfile.ClearEvents();
    }

    public Task<IEnumerable<AvgPriceProfile>> GetAvgPriceProfilesAsync()
    {
        var lines = _localDatabase.GetAvgPriceLines().FindAll().ToList().GroupBy(x => x.ProfileId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var response = _localDatabase.GetAvgPriceProfiles().FindAll()
            .Select(profile => profile.AsDomainObject(lines[profile.Id]));

        return Task.FromResult(response);
    }

    public async Task DeleteAvgPriceProfileAsync(AvgPriceProfile avgPriceProfile)
    {
        _localDatabase.GetAvgPriceLines().DeleteMany(x => x.ProfileId == new ObjectId(avgPriceProfile.Id.ToString()));
        _localDatabase.GetAvgPriceProfiles().Delete(new ObjectId(avgPriceProfile.Id.ToString()));

        await _domainEventPublisher.PublishAsync(new AvgPriceProfileDeletedEvent(avgPriceProfile));
    }
}