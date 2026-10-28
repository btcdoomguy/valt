using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.Notifications;
using Valt.Infra.Modules.Reports.Notifications.Handlers;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class ReportDataProviderCacheTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fiatAccount = new FiatAccountBuilder()
        {
            Id = _fiatAccountId,
            Name = "Fiat Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1
        }.Build();
        _localDatabase.GetAccounts().Insert(fiatAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("General")
            .WithIcon(Icon.Empty)
            .Build();
        _localDatabase.GetCategories().Insert(category);

        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity()
            {
                Date = currentDate,
                Price = 100000m
            });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity()
            {
                Date = currentDate,
                Currency = FiatCurrency.Brl.Code,
                Price = 5.5m
            });

            currentDate = currentDate.AddDays(1);
        }

        await base.SeedDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        _localDatabase.GetTransactions().DeleteAll();
    }

    [Test]
    public async Task CreateAsync_Without_Invalidate_Serves_Stale_Data_When_Count_Unchanged()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);

        var transaction = BuildExpense(isExcluded: false);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var provider1 = await factory.CreateAsync();
        Assert.That(provider1.AllTransactions.Single().IsExcluded, Is.False);

        // A flag toggle is an Upsert with an unchanged transaction count —
        // the count-based cache cannot detect it
        transaction.SetExcluded(true);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var provider2 = await factory.CreateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(ReferenceEquals(provider2, provider1), Is.True);
            Assert.That(provider2.AllTransactions.Single().IsExcluded, Is.False);
        });
    }

    [Test]
    public async Task Invalidate_Then_CreateAsync_Returns_Fresh_Provider_Reflecting_Flag_Toggle()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);

        var transaction = BuildExpense(isExcluded: false);
        await _transactionRepository.SaveTransactionAsync(transaction);

        var provider1 = await factory.CreateAsync();

        transaction.SetExcluded(true);
        await _transactionRepository.SaveTransactionAsync(transaction);

        factory.Invalidate();
        var provider2 = await factory.CreateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(ReferenceEquals(provider2, provider1), Is.False);
            Assert.That(provider2.AllTransactions.Single().IsExcluded, Is.True);
        });
    }

    [Test]
    public async Task SaveTransactionAsync_Publishes_TransactionsDataChangedNotification()
    {
        var publisher = Substitute.For<INotificationPublisher>();
        var repository = new TransactionRepository(
            _localDatabase, _priceDatabase, Substitute.For<IDomainEventPublisher>(), publisher);

        var transaction = BuildExpense(isExcluded: false);
        await repository.SaveTransactionAsync(transaction);

        await publisher.Received(1).PublishAsync(Arg.Any<TransactionsDataChangedNotification>());
    }

    [Test]
    public async Task DeleteTransactionAsync_Publishes_TransactionsDataChangedNotification()
    {
        var publisher = Substitute.For<INotificationPublisher>();
        var repository = new TransactionRepository(
            _localDatabase, _priceDatabase, Substitute.For<IDomainEventPublisher>(), publisher);

        var transaction = BuildExpense(isExcluded: false);
        await repository.SaveTransactionAsync(transaction);

        publisher.ClearReceivedCalls();
        await repository.DeleteTransactionAsync(transaction.Id);

        await publisher.Received(1).PublishAsync(Arg.Any<TransactionsDataChangedNotification>());
    }

    [Test]
    public async Task TransactionsDataChangedHandler_Calls_Invalidate_On_Factory()
    {
        var factory = Substitute.For<IReportDataProviderFactory>();
        var handler = new TransactionsDataChangedHandler(factory);

        await handler.HandleAsync(new TransactionsDataChangedNotification());

        factory.Received(1).Invalidate();
    }

    [Test]
    public async Task Flag_Toggle_Through_Publish_Handler_Chain_Refreshes_Provider_Without_Restart()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);
        var handler = new TransactionsDataChangedHandler(factory);
        var publisher = new InlineNotificationPublisher(handler);
        var repository = new TransactionRepository(
            _localDatabase, _priceDatabase, Substitute.For<IDomainEventPublisher>(), publisher);

        var transaction = BuildExpense(isExcluded: false);
        await repository.SaveTransactionAsync(transaction);

        var provider1 = await factory.CreateAsync();
        Assert.That(provider1.AllTransactions.Single().IsExcluded, Is.False);

        // Flag-only toggle: repository Upsert with unchanged transaction count.
        // The save publishes TransactionsDataChangedNotification, the handler invalidates
        // the factory, and the next CreateAsync returns a provider reflecting the toggle —
        // the same chain the app runs, with no app-restart equivalent.
        transaction.SetExcluded(true);
        await repository.SaveTransactionAsync(transaction);

        var provider2 = await factory.CreateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(ReferenceEquals(provider2, provider1), Is.False);
            Assert.That(provider2.AllTransactions.Single().IsExcluded, Is.True);
        });
    }

    [Test]
    public void SaveTransactionAsync_Propagates_NotificationPublisher_Failure()
    {
        var publisher = Substitute.For<INotificationPublisher>();
        publisher.PublishAsync(Arg.Any<TransactionsDataChangedNotification>())
            .Returns(Task.FromException(new InvalidOperationException("handler fault")));
        var repository = new TransactionRepository(
            _localDatabase, _priceDatabase, Substitute.For<IDomainEventPublisher>(), publisher);

        var transaction = BuildExpense(isExcluded: false);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.SaveTransactionAsync(transaction));
    }

    [Test]
    public void DeleteTransactionAsync_Propagates_NotificationPublisher_Failure()
    {
        var publisher = Substitute.For<INotificationPublisher>();
        publisher.PublishAsync(Arg.Any<TransactionsDataChangedNotification>())
            .Returns(Task.FromException(new InvalidOperationException("handler fault")));
        var repository = new TransactionRepository(
            _localDatabase, _priceDatabase, Substitute.For<IDomainEventPublisher>(), publisher);

        var transaction = BuildExpense(isExcluded: false);
        // The first save also routes through the (faulting) publisher; swallow it so the
        // delete under test starts from a persisted transaction.
        Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.SaveTransactionAsync(transaction));

        Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.DeleteTransactionAsync(transaction.Id));
    }

    /// <summary>
    /// Dispatches notifications synchronously to the real handler, standing in for the app's
    /// DI-mediated publish pipeline so the repository → publisher → handler → factory chain
    /// is exercised end to end.
    /// </summary>
    private sealed class InlineNotificationPublisher(TransactionsDataChangedHandler handler) : INotificationPublisher
    {
        public async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event) where TIntegrationEvent : class, INotification
        {
            if (@event is TransactionsDataChangedNotification notification)
                await handler.HandleAsync(notification);
        }
    }

    private Transaction BuildExpense(bool isExcluded)
    {
        return new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2025, 6, 15),
            Name = "Test Expense",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_fiatAccountId, 100m, false),
            IsExcluded = isExcluded
        }.BuildDomainObject();
    }
}
