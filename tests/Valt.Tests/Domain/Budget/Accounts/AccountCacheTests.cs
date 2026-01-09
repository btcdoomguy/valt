using Microsoft.Extensions.DependencyInjection;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Accounts.Services;

namespace Valt.Tests.Domain.Budget.Accounts;

[TestFixture]
public class AccountCacheTests : IntegrationTest
{
    [Test]
    public async Task Should_Register_AccountCache_Properly()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();

        var account = FiatAccount.New("Test", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account);

        var transaction = Transaction.New(new DateOnly(2025, 1, 1), "My Transaction", new CategoryId(),
            new FiatDetails(account.Id, 200m, true), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(1200m));
    }

    [Test]
    public async Task Should_Change_AccountCache_Properly()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();

        var account = FiatAccount.New("Test", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account);

        var transaction = Transaction.New(new DateOnly(2025, 1, 1), "My Transaction", new CategoryId(),
            new FiatDetails(account.Id, 200m, true), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(1200m));

        //change the transaction data value
        transaction.ChangeTransactionDetails(new FiatDetails(account.Id, 100m, false));
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1CacheUpdated = accountCaches.SingleOrDefault(x => x.Id == account.Id.AsObjectId());

        Assert.That(account1CacheUpdated.Total, Is.EqualTo(900m));
    }

    [Test]
    public async Task Should_Change_AccountCache_Properly_ToAnotherAccount()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();

        var account1 = FiatAccount.New("Test 1", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account1);

        var account2 = FiatAccount.New("Test 2", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account2);

        var transaction = Transaction.New(new DateOnly(2025, 1, 1), "My Transaction", new CategoryId(),
            new FiatDetails(account1.Id, 200m, true), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());
        var account2Cache = accountCaches.SingleOrDefault(x => x.Id == account2.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(1200m));
        Assert.That(account2Cache.Total, Is.EqualTo(1000m));

        //change the transaction data value and account
        transaction.ChangeTransactionDetails(new FiatDetails(account2.Id, 100m, false));
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1CacheUpdated = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());
        var account2CacheUpdated = accountCaches.SingleOrDefault(x => x.Id == account2.Id.AsObjectId());

        Assert.That(account1CacheUpdated.Total, Is.EqualTo(1000m));
        Assert.That(account2CacheUpdated.Total, Is.EqualTo(900m));
    }

    [Test]
    public async Task Should_Change_AccountCache_Properly_WhenTransfer()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();

        var account1 = FiatAccount.New("Test 1", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account1);

        var account2 = FiatAccount.New("Test 2", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account2);

        var transaction = Transaction.New(new DateOnly(2025, 1, 1), "My Transaction", new CategoryId(),
            new FiatToFiatDetails(account1.Id, account2.Id, 200m, 200m), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());
        var account2Cache = accountCaches.SingleOrDefault(x => x.Id == account2.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(800m));
        Assert.That(account2Cache.Total, Is.EqualTo(1200m));

        //change the transaction data value and account
        transaction.ChangeTransactionDetails(new FiatToFiatDetails(account2.Id, account1.Id, 200m, 200m));
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1CacheUpdated = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());
        var account2CacheUpdated = accountCaches.SingleOrDefault(x => x.Id == account2.Id.AsObjectId());

        Assert.That(account1CacheUpdated.Total, Is.EqualTo(1200));
        Assert.That(account2CacheUpdated.Total, Is.EqualTo(800m));
    }

    [Test]
    public async Task Should_Set_AccountCache_Current_And_Future_Properly()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var account1 = FiatAccount.New("Test 1", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account1);

        var transaction = Transaction.New(today.AddDays(5), "My Transaction", new CategoryId(),
            new FiatDetails(account1.Id, 200m, true), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(1200));
        Assert.That(DateOnly.FromDateTime(account1Cache.CurrentDate.ToUniversalTime()), Is.EqualTo(today));
        Assert.That(account1Cache.CurrentTotal, Is.EqualTo(1000));
    }

    [Test]
    public async Task Should_Set_AccountCache_Current_And_Future_Properly_After_Changing_Date()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var account1 = FiatAccount.New("Test 1", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account1);

        var transaction = Transaction.New(today.AddDays(5), "My Transaction", new CategoryId(),
            new FiatDetails(account1.Id, 200m, true), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(1200));
        Assert.That(DateOnly.FromDateTime(account1Cache.CurrentDate.ToUniversalTime()), Is.EqualTo(today));
        Assert.That(account1Cache.CurrentTotal, Is.EqualTo(1000));

        transaction.ChangeDate(today);

        await transactionRepository.SaveTransactionAsync(transaction);

        accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(1200));
        Assert.That(DateOnly.FromDateTime(account1Cache.CurrentDate.ToUniversalTime()), Is.EqualTo(today));
        Assert.That(account1Cache.CurrentTotal, Is.EqualTo(1200));
    }

    [Test]
    public async Task Should_Set_AccountCache_Current_And_Future_Properly_After_Changing_Date_And_Values()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var account1 = FiatAccount.New("Test 1", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account1);

        var transaction = Transaction.New(today.AddDays(5), "My Transaction", new CategoryId(),
            new FiatDetails(account1.Id, 200m, true), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(1200));
        Assert.That(DateOnly.FromDateTime(account1Cache.CurrentDate.ToUniversalTime()), Is.EqualTo(today));
        Assert.That(account1Cache.CurrentTotal, Is.EqualTo(1000));

        transaction.ChangeDate(today);
        transaction.ChangeTransactionDetails(new FiatDetails(account1.Id, 100m, false));

        await transactionRepository.SaveTransactionAsync(transaction);

        accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(900));
        Assert.That(DateOnly.FromDateTime(account1Cache.CurrentDate.ToUniversalTime()), Is.EqualTo(today));
        Assert.That(account1Cache.CurrentTotal, Is.EqualTo(900));
    }
    
    [Test]
    public async Task Should_Set_AccountCache_Current_And_Recalculate_Properly_When_Today_Changes()
    {
        var accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var account1 = FiatAccount.New("Test 1", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        await accountRepository.SaveAccountAsync(account1);

        var transaction = Transaction.New(today.AddDays(5), "My Transaction", new CategoryId(),
            new FiatDetails(account1.Id, 200m, false), null, null);
        await transactionRepository.SaveTransactionAsync(transaction);

        //retrieve data from accountCache
        var accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        var account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(800));
        Assert.That(DateOnly.FromDateTime(account1Cache.CurrentDate.ToUniversalTime()), Is.EqualTo(today));
        Assert.That(account1Cache.CurrentTotal, Is.EqualTo(1000));

        var service = _serviceProvider.GetRequiredService<IAccountCacheService>();

        await service.RefreshCurrentTotalsAsync(today.AddDays(5));

        accountCaches = localDatabase.GetAccountCaches().FindAll().ToList();

        account1Cache = accountCaches.SingleOrDefault(x => x.Id == account1.Id.AsObjectId());

        Assert.That(account1Cache.Total, Is.EqualTo(800));
        Assert.That(DateOnly.FromDateTime(account1Cache.CurrentDate.ToUniversalTime()), Is.EqualTo(today.AddDays(5)));
        Assert.That(account1Cache.CurrentTotal, Is.EqualTo(800));
    }
}