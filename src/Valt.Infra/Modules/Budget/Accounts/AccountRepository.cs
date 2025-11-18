using LiteDB;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Accounts.Exceptions;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Accounts;

internal class AccountRepository : IAccountRepository
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public AccountRepository(ILocalDatabase localDatabase,
        IDomainEventPublisher domainEventPublisher)
    {
        _localDatabase = localDatabase;
        _domainEventPublisher = domainEventPublisher;
    }

    public Task<Account?> GetAccountByIdAsync(AccountId accountId)
    {
        var entity = _localDatabase.GetAccounts().FindById(new ObjectId(accountId));

        return Task.FromResult(entity?.AsDomainObject());
    }

    public async Task SaveAccountAsync(Account account)
    {
        var entity = account.AsEntity();

        _localDatabase.GetAccounts().Upsert(entity);

        foreach (var @event in account.Events)
            await _domainEventPublisher.PublishAsync(@event);

        account.ClearEvents();
    }

    public Task<IList<Account>> GetAccountsAsync()
    {
        var entities = _localDatabase.GetAccounts().FindAll()!;

        var accounts = entities.Select(entity => entity.AsDomainObject()).ToList();

        return Task.FromResult<IList<Account>>(accounts);
    }

    public Task DeleteAccountAsync(Account account)
    {
        var accountIdBson = new ObjectId(account.Id.Value);
        var anyTransaction = _localDatabase.GetTransactions()
            .FindOne(x => x.FromAccountId == accountIdBson || x.ToAccountId == accountIdBson);

        if (anyTransaction is not null)
            throw new AccountHasTransactionsException();

        _localDatabase.GetAccounts().Delete(accountIdBson);

        return Task.CompletedTask;
    }


}