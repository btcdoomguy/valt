using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Valt.Core.Common;
using Valt.Infra;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.DataSources.Fiat;
using LiteDB;

namespace Valt.Tests.Jobs;

[TestFixture]
public class FiatHistoryUpdaterJobTests
{
    private IFiatHistoricalDataProvider _initialSeedProvider = null!;
    private IFiatHistoricalDataProvider _regularProvider = null!;
#pragma warning disable NUnit1032 // These are NSubstitute mocks, not real implementations
    private IPriceDatabase _priceDatabase = null!;
    private ILocalDatabase _localDatabase = null!;
#pragma warning restore NUnit1032

    [SetUp]
    public void SetUp()
    {
        // Setup mock databases
        _priceDatabase = Substitute.For<IPriceDatabase>();
        _localDatabase = Substitute.For<ILocalDatabase>();

        // Setup mock providers
        _initialSeedProvider = Substitute.For<IFiatHistoricalDataProvider>();
        _initialSeedProvider.Name.Returns("StaticCsv");
        _initialSeedProvider.InitialDownloadSource.Returns(true);
        _initialSeedProvider.SupportedCurrencies.Returns(new HashSet<FiatCurrency>
        {
            FiatCurrency.Brl, FiatCurrency.Eur, FiatCurrency.Gbp
        });

        _regularProvider = Substitute.For<IFiatHistoricalDataProvider>();
        _regularProvider.Name.Returns("Frankfurter");
        _regularProvider.InitialDownloadSource.Returns(false);
        _regularProvider.SupportedCurrencies.Returns(new HashSet<FiatCurrency>
        {
            FiatCurrency.Brl, FiatCurrency.Eur, FiatCurrency.Gbp
        });

        // Setup local database mock for ConfigurationManager
        _localDatabase.HasDatabaseOpen.Returns(true);

        // Setup price database
        _priceDatabase.HasDatabaseOpen.Returns(true);
    }

    private ConfigurationManager CreateConfigurationManager(List<string> currencies)
    {
        var configCollection = Substitute.For<ILiteCollection<ConfigurationEntity>>();

        if (currencies.Count > 0)
        {
            var configEntity = new ConfigurationEntity
            {
                Key = ConfigurationKeys.AvailableFiatCurrencies,
                Value = string.Join(",", currencies)
            };
            configCollection.FindOne(Arg.Any<System.Linq.Expressions.Expression<Func<ConfigurationEntity, bool>>>())
                .Returns(configEntity);
        }
        else
        {
            configCollection.FindOne(Arg.Any<System.Linq.Expressions.Expression<Func<ConfigurationEntity, bool>>>())
                .Returns((ConfigurationEntity?)null);

            // Return empty accounts for auto-population
            var accountsCollection = Substitute.For<ILiteCollection<AccountEntity>>();
            accountsCollection.FindAll().Returns(new List<AccountEntity>());
            _localDatabase.GetAccounts().Returns(accountsCollection);
        }

        _localDatabase.GetConfiguration().Returns(configCollection);

        return new ConfigurationManager(_localDatabase);
    }

    #region Provider Selection Tests

    [Test]
    public async Task Should_Use_InitialSeedProvider_When_No_Data_Exists_For_Currency()
    {
        // Arrange: Configure currencies with no existing data
        var configManager = CreateConfigurationManager(new List<string> { FiatCurrency.Brl.Code });

        var fiatDataCollection = Substitute.For<ILiteCollection<FiatDataEntity>>();
        fiatDataCollection.Exists(Arg.Any<System.Linq.Expressions.Expression<Func<FiatDataEntity, bool>>>()).Returns(false);
        fiatDataCollection.Query().Returns(Substitute.For<ILiteQueryable<FiatDataEntity>>());
        fiatDataCollection.FindAll().Returns(new List<FiatDataEntity>());

        _priceDatabase.GetFiatData().Returns(fiatDataCollection);

        var transactionsCollection = Substitute.For<ILiteCollection<Infra.Modules.Budget.Transactions.TransactionEntity>>();
        transactionsCollection.FindAll().Returns(new List<Infra.Modules.Budget.Transactions.TransactionEntity>());
        _localDatabase.GetTransactions().Returns(transactionsCollection);

        _initialSeedProvider.GetPricesAsync(
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new List<IFiatHistoricalDataProvider.FiatPriceData>
            {
                new(new DateOnly(2024, 1, 2), new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>
                {
                    new(FiatCurrency.Brl, 5.0m)
                })
            });

        var providers = new List<IFiatHistoricalDataProvider> { _initialSeedProvider, _regularProvider };

        var job = new FiatHistoryUpdaterJob(
            _priceDatabase,
            _localDatabase,
            providers,
            configManager,
            new NullLogger<FiatHistoryUpdaterJob>());

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert: Initial seed provider should be called
        await _initialSeedProvider.Received(1).GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Is<IEnumerable<FiatCurrency>>(c => c.Contains(FiatCurrency.Brl)));

        // Regular provider should not be called
        await _regularProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());
    }

    [Test]
    public async Task Should_Use_RegularProvider_When_Data_Already_Exists_For_Currency()
    {
        // Arrange: Configure currencies with existing data
        var configManager = CreateConfigurationManager(new List<string> { FiatCurrency.Brl.Code });

        var existingData = new List<FiatDataEntity>
        {
            new()
            {
                Currency = FiatCurrency.Brl.Code,
                Date = new DateOnly(2024, 1, 1).ToValtDateTime(),
                Price = 5.0m
            }
        };

        var fiatDataCollection = Substitute.For<ILiteCollection<FiatDataEntity>>();
        fiatDataCollection.Exists(Arg.Any<System.Linq.Expressions.Expression<Func<FiatDataEntity, bool>>>()).Returns(true);
        var queryable = Substitute.For<ILiteQueryable<FiatDataEntity>>();
        queryable.Count().Returns(existingData.Count);
        fiatDataCollection.Query().Returns(queryable);
        fiatDataCollection.FindAll().Returns(existingData);
        fiatDataCollection.Find(Arg.Any<System.Linq.Expressions.Expression<Func<FiatDataEntity, bool>>>()).Returns(existingData);

        _priceDatabase.GetFiatData().Returns(fiatDataCollection);

        var transactionsCollection = Substitute.For<ILiteCollection<Infra.Modules.Budget.Transactions.TransactionEntity>>();
        transactionsCollection.FindAll().Returns(new List<Infra.Modules.Budget.Transactions.TransactionEntity>());
        _localDatabase.GetTransactions().Returns(transactionsCollection);

        _regularProvider.GetPricesAsync(
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new List<IFiatHistoricalDataProvider.FiatPriceData>
            {
                new(new DateOnly(2024, 1, 3), new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>
                {
                    new(FiatCurrency.Brl, 5.1m)
                })
            });

        var providers = new List<IFiatHistoricalDataProvider> { _initialSeedProvider, _regularProvider };

        var job = new FiatHistoryUpdaterJob(
            _priceDatabase,
            _localDatabase,
            providers,
            configManager,
            new NullLogger<FiatHistoryUpdaterJob>());

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert: Regular provider should be called
        await _regularProvider.Received().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Is<IEnumerable<FiatCurrency>>(c => c.Contains(FiatCurrency.Brl)));

        // Initial seed provider should not be called
        await _initialSeedProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());
    }

    [Test]
    public async Task Should_Use_Both_Providers_When_Some_Currencies_Have_Data_And_Some_Do_Not()
    {
        // Arrange: Configure two currencies - one with data, one without
        var configManager = CreateConfigurationManager(new List<string>
        {
            FiatCurrency.Brl.Code,
            FiatCurrency.Eur.Code
        });

        var existingData = new List<FiatDataEntity>
        {
            new()
            {
                Currency = FiatCurrency.Brl.Code,
                Date = new DateOnly(2024, 1, 1).ToValtDateTime(),
                Price = 5.0m
            }
        };

        var fiatDataCollection = Substitute.For<ILiteCollection<FiatDataEntity>>();
        // BRL has data, EUR doesn't
        fiatDataCollection.Exists(Arg.Is<System.Linq.Expressions.Expression<Func<FiatDataEntity, bool>>>(
            expr => expr.Compile().Invoke(new FiatDataEntity { Currency = FiatCurrency.Brl.Code }))).Returns(true);
        fiatDataCollection.Exists(Arg.Is<System.Linq.Expressions.Expression<Func<FiatDataEntity, bool>>>(
            expr => expr.Compile().Invoke(new FiatDataEntity { Currency = FiatCurrency.Eur.Code }))).Returns(false);

        var queryable = Substitute.For<ILiteQueryable<FiatDataEntity>>();
        queryable.Count().Returns(existingData.Count);
        fiatDataCollection.Query().Returns(queryable);
        fiatDataCollection.FindAll().Returns(existingData);
        fiatDataCollection.Find(Arg.Any<System.Linq.Expressions.Expression<Func<FiatDataEntity, bool>>>()).Returns(existingData);

        _priceDatabase.GetFiatData().Returns(fiatDataCollection);

        var transactionsCollection = Substitute.For<ILiteCollection<Infra.Modules.Budget.Transactions.TransactionEntity>>();
        transactionsCollection.FindAll().Returns(new List<Infra.Modules.Budget.Transactions.TransactionEntity>());
        _localDatabase.GetTransactions().Returns(transactionsCollection);

        _initialSeedProvider.GetPricesAsync(
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new List<IFiatHistoricalDataProvider.FiatPriceData>
            {
                new(new DateOnly(2024, 1, 2), new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>
                {
                    new(FiatCurrency.Eur, 0.9m)
                })
            });

        _regularProvider.GetPricesAsync(
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new List<IFiatHistoricalDataProvider.FiatPriceData>
            {
                new(new DateOnly(2024, 1, 3), new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>
                {
                    new(FiatCurrency.Brl, 5.1m)
                })
            });

        var providers = new List<IFiatHistoricalDataProvider> { _initialSeedProvider, _regularProvider };

        var job = new FiatHistoryUpdaterJob(
            _priceDatabase,
            _localDatabase,
            providers,
            configManager,
            new NullLogger<FiatHistoryUpdaterJob>());

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert: Initial seed provider should be called for EUR (no data)
        await _initialSeedProvider.Received().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Is<IEnumerable<FiatCurrency>>(c => c.Contains(FiatCurrency.Eur) && !c.Contains(FiatCurrency.Brl)));

        // Regular provider should be called for BRL (has data)
        await _regularProvider.Received().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Is<IEnumerable<FiatCurrency>>(c => c.Contains(FiatCurrency.Brl) && !c.Contains(FiatCurrency.Eur)));
    }

    #endregion

    #region InitialDownloadSource Property Tests

    [Test]
    public void StaticCsvProvider_Should_Have_InitialDownloadSource_True()
    {
        // Arrange & Act
        var provider = new Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers.StaticCsvFiatHistoricalDataProvider(
            new NullLogger<Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers.StaticCsvFiatHistoricalDataProvider>());

        // Assert
        Assert.That(provider.InitialDownloadSource, Is.True);
    }

    [Test]
    public void FrankfurterProvider_Should_Have_InitialDownloadSource_False()
    {
        // Arrange & Act
        var provider = new Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers.FrankfurterFiatHistoricalDataProvider(
            new NullLogger<Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers.FrankfurterFiatHistoricalDataProvider>());

        // Assert
        Assert.That(provider.InitialDownloadSource, Is.False);
    }

    #endregion

    #region Provider Selection Edge Cases

    [Test]
    public async Task Should_Not_Call_Any_Provider_When_No_Currencies_Configured()
    {
        // Arrange: No currencies configured
        var configManager = CreateConfigurationManager(new List<string>());

        var providers = new List<IFiatHistoricalDataProvider> { _initialSeedProvider, _regularProvider };

        var job = new FiatHistoryUpdaterJob(
            _priceDatabase,
            _localDatabase,
            providers,
            configManager,
            new NullLogger<FiatHistoryUpdaterJob>());

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert: No provider should be called
        await _initialSeedProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());

        await _regularProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());
    }

    [Test]
    public async Task Should_Skip_When_Price_Database_Not_Open()
    {
        // Arrange
        _priceDatabase.HasDatabaseOpen.Returns(false);
        var configManager = CreateConfigurationManager(new List<string> { FiatCurrency.Brl.Code });

        var providers = new List<IFiatHistoricalDataProvider> { _initialSeedProvider, _regularProvider };

        var job = new FiatHistoryUpdaterJob(
            _priceDatabase,
            _localDatabase,
            providers,
            configManager,
            new NullLogger<FiatHistoryUpdaterJob>());

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert: No provider should be called
        await _initialSeedProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());

        await _regularProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());
    }

    [Test]
    public async Task Should_Skip_When_Local_Database_Not_Open()
    {
        // Arrange
        _localDatabase.HasDatabaseOpen.Returns(false);
        var configManager = CreateConfigurationManager(new List<string> { FiatCurrency.Brl.Code });

        var providers = new List<IFiatHistoricalDataProvider> { _initialSeedProvider, _regularProvider };

        var job = new FiatHistoryUpdaterJob(
            _priceDatabase,
            _localDatabase,
            providers,
            configManager,
            new NullLogger<FiatHistoryUpdaterJob>());

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert: No provider should be called
        await _initialSeedProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());

        await _regularProvider.DidNotReceive().GetPricesAsync(
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<IEnumerable<FiatCurrency>>());
    }

    #endregion
}
