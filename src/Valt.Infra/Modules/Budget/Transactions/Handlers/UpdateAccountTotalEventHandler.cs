using LiteDB;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Events;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Core.Modules.Budget.Transactions.Events;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Budget.Transactions.Handlers;

internal class UpdateAccountTotalEventHandler(ILocalDatabase localDatabase, IClock clock)
    : IDomainEventHandler<TransactionCreatedEvent>,
        IDomainEventHandler<TransactionDeletedEvent>,
        IDomainEventHandler<TransactionDetailsChangedEvent>,
        IDomainEventHandler<TransactionDateChangedEvent>,
        IDomainEventHandler<AccountCreatedEvent>,
        IDomainEventHandler<AccountInitialAmountChangedEvent>,
        IDomainEventHandler<AccountDeletedEvent>
{
    public Task HandleAsync(TransactionCreatedEvent @event)
    {
        ChangeTotalsForTransactionDetails(@event.Transaction.Date, @event.Transaction.TransactionDetails, true);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransactionDeletedEvent @event)
    {
        ChangeTotalsForTransactionDetails(@event.Transaction.Date, @event.Transaction.TransactionDetails, false);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransactionDetailsChangedEvent @event)
    {
        ChangeTotalsForTransactionDetails(@event.Transaction.Date, @event.PreviousDetails, false);
        ChangeTotalsForTransactionDetails(@event.Transaction.Date, @event.Transaction.TransactionDetails, true);
        return Task.CompletedTask;
    }

    public Task HandleAsync(AccountInitialAmountChangedEvent @event)
    {
        var account = @event.Account;
        var accountBsonId = account.Id.ToObjectId();
        var accountEntity = localDatabase.GetAccounts().FindById(accountBsonId);
        var accountCache = localDatabase.GetAccountCaches()
            .FindById(accountBsonId);

        if (accountCache is null) return Task.CompletedTask;
        
        var amountToSubtract = account.AccountType == AccountTypes.Bitcoin
            ? @event.PreviousBtcInitialAmount!.Sats
            : @event.PreviousFiatInitialAmount!.Value;

        accountCache.Total -= amountToSubtract;
        accountCache.CurrentTotal -= amountToSubtract;
        
        accountCache.Total += accountEntity.InitialAmount;
        accountCache.CurrentTotal += accountEntity.InitialAmount;

        localDatabase.GetAccountCaches().Update(accountCache);

        return Task.CompletedTask;
    }

    public Task HandleAsync(AccountCreatedEvent @event)
    {
        var accountBsonId = new ObjectId(@event.Account.Id.Value);
        var accountEntity = localDatabase.GetAccounts().FindById(accountBsonId);
        var accountCache = new AccountCacheEntity()
        {
            Id = @event.Account.Id.AsObjectId(),
            Total = accountEntity.InitialAmount,
            CurrentTotal = accountEntity.InitialAmount,
            CurrentDate = clock.GetCurrentLocalDate().ToValtDateTime()
        };

        localDatabase.GetAccountCaches().Insert(accountCache);
        return Task.CompletedTask;
    }

    public Task HandleAsync(AccountDeletedEvent @event)
    {
        var account = @event.Account;
        var accountBsonId = new ObjectId(account.Id.Value);
        localDatabase.GetAccountCaches().Delete(accountBsonId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransactionDateChangedEvent @event)
    {
        ChangeTotalsForTransactionDateChanges(@event.PreviousDate, @event.Transaction.Date, @event.TransactionDetails);
        return Task.CompletedTask;
    }
    
    private void ChangeTotalsForTransactionDetails(DateOnly transactionDate, TransactionDetails transactionDetails, bool add)
    {
        var today = clock.GetCurrentLocalDate();

        //change the origin account totals
        var fromAccountBsonId = new ObjectId(transactionDetails.FromAccountId.Value);
        var fromAccountEntity = localDatabase.GetAccounts().FindById(fromAccountBsonId);
        var fromAccountCache = localDatabase.GetAccountCaches()
            .FindById(fromAccountBsonId);

        if (fromAccountCache is not null)
        {
            if (fromAccountEntity.AccountEntityType == AccountEntityType.Fiat)
            {
                var value = add
                    ? transactionDetails.FromAccountFiatValue.GetValueOrDefault()
                    : -transactionDetails.FromAccountFiatValue.GetValueOrDefault();
                fromAccountCache.Total += value;

                if (transactionDate <= today)
                    fromAccountCache.CurrentTotal += value;
            }
            else
            {
                var value = add
                    ? transactionDetails.FromAccountSatsValue.GetValueOrDefault()
                    : -transactionDetails.FromAccountSatsValue.GetValueOrDefault();
                fromAccountCache.Total += value;

                if (transactionDate <= today)
                    fromAccountCache.CurrentTotal += value;
            }

            localDatabase.GetAccountCaches().Update(fromAccountCache);
        }

        if (transactionDetails.ToAccountId is null)
            return;

        //change the target account totals
        var toAccountBsonId = new ObjectId(transactionDetails.ToAccountId.Value);
        var toAccountEntity = localDatabase.GetAccounts().FindById(toAccountBsonId);
        var toAccountCache = localDatabase.GetAccountCaches()
            .FindById(toAccountBsonId);

        if (toAccountCache is not null)
        {
            if (toAccountEntity.AccountEntityType == AccountEntityType.Fiat)
            {
                var value = add
                    ? transactionDetails.ToAccountFiatValue.GetValueOrDefault()
                    : -transactionDetails.ToAccountFiatValue.GetValueOrDefault();
                toAccountCache.Total += value;
                
                if (transactionDate <= today)
                    toAccountCache.CurrentTotal += value;
            }
            else
            {
                var value = add
                    ? transactionDetails.ToAccountSatsValue.GetValueOrDefault()
                    : -transactionDetails.ToAccountSatsValue.GetValueOrDefault();
                toAccountCache.Total += value;
                
                if (transactionDate <= today)
                    toAccountCache.CurrentTotal += value;
            }

            localDatabase.GetAccountCaches().Update(toAccountCache);
        }
    }
    
    private void ChangeTotalsForTransactionDateChanges(DateOnly previousTransactionDate, DateOnly transactionDate, TransactionDetails transactionDetails)
    {
        var today = clock.GetCurrentLocalDate();
        
        //only needs to refresh if the date changed outside current total range
        if (previousTransactionDate <= today && transactionDate <= today)
            return;

        if (previousTransactionDate > today && transactionDate > today)
            return;
        
        var removeFromCurrentTotal = transactionDate > today;
        
        //change the origin account totals
        var fromAccountBsonId = new ObjectId(transactionDetails.FromAccountId.Value);
        var fromAccountEntity = localDatabase.GetAccounts().FindById(fromAccountBsonId);
        var fromAccountCache = localDatabase.GetAccountCaches()
            .FindById(fromAccountBsonId);

        if (fromAccountCache is not null)
        {
            if (fromAccountEntity.AccountEntityType == AccountEntityType.Fiat)
            {
                var value = transactionDetails.FromAccountFiatValue.GetValueOrDefault();
                
                if (removeFromCurrentTotal)
                    fromAccountCache.CurrentTotal -= value;
                else
                    fromAccountCache.CurrentTotal += value;
            }
            else
            {
                var value = transactionDetails.FromAccountSatsValue.GetValueOrDefault();
                
                if (removeFromCurrentTotal)
                    fromAccountCache.CurrentTotal -= value;
                else
                    fromAccountCache.CurrentTotal += value;
            }

            localDatabase.GetAccountCaches().Update(fromAccountCache);
        }

        if (transactionDetails.ToAccountId is null)
            return;

        //change the target account totals
        var toAccountBsonId = new ObjectId(transactionDetails.ToAccountId.Value);
        var toAccountEntity = localDatabase.GetAccounts().FindById(toAccountBsonId);
        var toAccountCache = localDatabase.GetAccountCaches()
            .FindById(toAccountBsonId);

        if (toAccountCache is not null)
        {
            if (toAccountEntity.AccountEntityType == AccountEntityType.Fiat)
            {
                var value = transactionDetails.ToAccountFiatValue.GetValueOrDefault();
                
                if (removeFromCurrentTotal)
                    toAccountCache.CurrentTotal -= value;
                else
                    toAccountCache.CurrentTotal += value;
            }
            else
            {
                var value = transactionDetails.ToAccountSatsValue.GetValueOrDefault();
                
                if (removeFromCurrentTotal)
                    toAccountCache.CurrentTotal -= value;
                else
                    toAccountCache.CurrentTotal += value;
            }

            localDatabase.GetAccountCaches().Update(toAccountCache);
        }
    }
}