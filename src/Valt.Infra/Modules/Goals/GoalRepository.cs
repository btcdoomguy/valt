using LiteDB;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Core.Modules.Goals.Events;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Goals;

internal sealed class GoalRepository : IGoalRepository
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public GoalRepository(ILocalDatabase localDatabase, IDomainEventPublisher domainEventPublisher)
    {
        _localDatabase = localDatabase;
        _domainEventPublisher = domainEventPublisher;
    }

    public Task<Goal?> GetByIdAsync(GoalId id)
    {
        var entity = _localDatabase.GetGoals().FindById(new ObjectId(id.ToString()));

        return Task.FromResult(entity?.AsDomainObject());
    }

    public async Task SaveAsync(Goal goal)
    {
        var entity = goal.AsEntity();
        _localDatabase.GetGoals().Upsert(entity);

        foreach (var @event in goal.Events)
        {
            await _domainEventPublisher.PublishAsync(@event);
        }

        goal.ClearEvents();
    }

    public Task<IEnumerable<Goal>> GetAllAsync()
    {
        var entities = _localDatabase.GetGoals().FindAll();
        return Task.FromResult(entities.Select(e => e.AsDomainObject()));
    }

    public async Task DeleteAsync(Goal goal)
    {
        _localDatabase.GetGoals().Delete(new ObjectId(goal.Id.ToString()));

        foreach (var @event in goal.Events)
        {
            await _domainEventPublisher.PublishAsync(@event);
        }

        await _domainEventPublisher.PublishAsync(new GoalDeletedEvent(goal));

        goal.ClearEvents();
    }

    public async Task MarkGoalsStaleForDateAsync(DateOnly date)
    {
        var goals = _localDatabase.GetGoals()
            .Find(x => x.IsUpToDate);

        foreach (var goalEntity in goals)
        {
            var goal = goalEntity.AsDomainObject();
            var range = goal.GetPeriodRange();

            if (date >= range.Start && date <= range.End)
            {
                goal.MarkAsStale();
                await SaveAsync(goal).ConfigureAwait(false);
            }
        }
    }
}
