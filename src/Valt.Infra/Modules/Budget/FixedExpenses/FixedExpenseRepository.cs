using LiteDB;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.FixedExpenses.Events;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

internal class FixedExpenseRepository : IFixedExpenseRepository
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public FixedExpenseRepository(ILocalDatabase localDatabase,
        IDomainEventPublisher domainEventPublisher)
    {
        _localDatabase = localDatabase;
        _domainEventPublisher = domainEventPublisher;
    }

    public Task<FixedExpense?> GetFixedExpenseByIdAsync(FixedExpenseId fixedExpenseId)
    {
        var entity = _localDatabase.GetFixedExpenses().FindById(new ObjectId(fixedExpenseId));

        var lastFixedExpenseRecord = _localDatabase.GetFixedExpenseRecords()
            .Find(x => x.FixedExpense != null && x.FixedExpense.Id == entity.Id).OrderByDescending(x => x.ReferenceDate)
            .FirstOrDefault();

        return Task.FromResult(entity?.AsDomainObject(lastFixedExpenseRecord));
    }

    public async Task SaveFixedExpenseAsync(FixedExpense fixedExpense)
    {
        var entity = fixedExpense.AsEntity();

        _localDatabase.GetFixedExpenses().Upsert(entity);

        foreach (var @event in fixedExpense.Events)
            await _domainEventPublisher.PublishAsync(@event);

        fixedExpense.ClearEvents();
    }

    public async Task DeleteFixedExpenseAsync(FixedExpenseId fixedExpenseId)
    {
        var fixedExpense = await GetFixedExpenseByIdAsync(fixedExpenseId);

        if (fixedExpense is null)
            return;

        var fixedExpenseIdBson = fixedExpenseId.ToObjectId();

        _localDatabase.GetFixedExpenses().Delete(fixedExpenseIdBson);

        await _domainEventPublisher.PublishAsync(new FixedExpenseDeletedEvent(fixedExpense));
    }
}