using NSubstitute;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Tests;

/// <summary>
/// Database tests are focused on small functionalities but without the initialized dependencies, so restricted to be
/// limited to back-and-forth from database without the collateral effects of the domain events
/// </summary>
public abstract class DatabaseTest
{
    protected MemoryStream _localDatabaseStream;
    protected ILocalDatabase _localDatabase;
    protected MemoryStream _priceDatabaseStream;
    protected IPriceDatabase _priceDatabase;

    protected IDomainEventPublisher _domainEventPublisher;
    
    protected ITransactionRepository _transactionRepository;
    protected IAccountRepository _accountRepository;
    protected ICategoryRepository _categoryRepository;

    protected DatabaseTest()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    private void RefreshLocalInstances()
    {
        _domainEventPublisher = Substitute.For<IDomainEventPublisher>();

        _transactionRepository = new TransactionRepository(_localDatabase, _domainEventPublisher);
        _categoryRepository = new CategoryRepository(_localDatabase);
        _accountRepository = new AccountRepository(_localDatabase, _domainEventPublisher);
    }

    [OneTimeSetUp]
    public async Task CreateTemporaryDb()
    {
        _localDatabaseStream = new MemoryStream();
        _localDatabase = new LocalDatabase(new Clock());
        _localDatabase.OpenInMemoryDatabase(_localDatabaseStream);
        
        _priceDatabaseStream = new MemoryStream();
        _priceDatabase = new PriceDatabase(new Clock(), Substitute.For<INotificationPublisher>());
        _priceDatabase.OpenInMemoryDatabase(_priceDatabaseStream);

        RefreshLocalInstances();

        await SeedDatabase();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _localDatabase.Dispose();
        _priceDatabase.Dispose();
    }

    [SetUp]
    public Task SetUp()
    {
        RefreshLocalInstances();
        return Task.CompletedTask;
    }

    protected virtual Task SeedDatabase()
    {
        return Task.CompletedTask;
    }

    [OneTimeTearDown]
    public async Task DestroyTemporaryDb()
    {
        _localDatabase.CloseDatabase();
        _priceDatabase.CloseDatabase();
        await _localDatabaseStream.DisposeAsync();
        await _priceDatabaseStream.DisposeAsync();
    }
}