using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Transactions;

internal class TransactionRepository : ITransactionRepository
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public TransactionRepository(ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        IDomainEventPublisher domainEventPublisher)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _domainEventPublisher = domainEventPublisher;
    }

    public Task<Transaction?> GetTransactionByIdAsync(TransactionId transactionId)
    {
        var entity = _localDatabase.GetTransactions().FindById(new ObjectId(transactionId));
        
        var fixedExpenseRelated = _localDatabase.GetFixedExpenseRecords().Find(x => x.Transaction != null && x.Transaction.Id == entity.Id);

        return Task.FromResult(entity?.AsDomainObject(fixedExpenseRelated.FirstOrDefault()));
    }

    public async Task SaveTransactionAsync(Transaction transaction)
    {
        var entity = transaction.AsEntity();

        _localDatabase.GetTransactions().Upsert(entity);

        // Check if transaction date is before the minimum fiat date in prices.db
        // If so, trigger a fiat history refresh to fetch the missing data
        CheckAndTriggerFiatHistoryRefresh(transaction.Date);

        foreach (var @event in transaction.Events)
            await _domainEventPublisher.PublishAsync(@event);

        transaction.ClearEvents();
    }

    private void CheckAndTriggerFiatHistoryRefresh(DateOnly transactionDate)
    {
        if (!_priceDatabase.HasDatabaseOpen)
            return;

        try
        {
            var fiatData = _priceDatabase.GetFiatData().FindAll().ToList();
            if (fiatData.Count == 0)
            {
                // No fiat data at all, trigger refresh
                WeakReferenceMessenger.Default.Send<FiatHistoryRefreshRequestedMessage>();
                return;
            }

            var minFiatDate = fiatData.Min(x => x.Date);
            var transactionDateTime = transactionDate.ToDateTime(TimeOnly.MinValue);
            if (transactionDateTime < minFiatDate)
            {
                // Transaction date is before the earliest fiat data, trigger refresh
                WeakReferenceMessenger.Default.Send<FiatHistoryRefreshRequestedMessage>();
            }
        }
        catch
        {
            // Ignore errors - the job will handle data fetching on its regular schedule
        }
    }

    public async Task DeleteTransactionAsync(TransactionId transactionId)
    {
        var transaction = await GetTransactionByIdAsync(transactionId);

        if (transaction is null)
            return;

        _localDatabase.GetTransactions().Delete(new ObjectId(transactionId));

        await _domainEventPublisher.PublishAsync(new TransactionDeletedEvent(transaction));
    }

    public Task<bool> HasAnyTransactionAsync(AccountId accountId)
    {
        var accountIdBson = new ObjectId(accountId.Value);
        var anyTransaction = _localDatabase.GetTransactions()
            .FindOne(x => x.FromAccountId == accountIdBson || x.ToAccountId == accountIdBson);

        return Task.FromResult(anyTransaction is not null);
    }
}