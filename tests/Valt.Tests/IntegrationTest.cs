using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Core;
using Valt.Core.Kernel.Factories;
using Valt.Infra;
using Valt.Infra.Crawlers.LivePriceCrawlers;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Kernel.Time;
using Valt.UI;
using Valt.UI.Base;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Views;
using Valt.UI.Views.Main;

namespace Valt.Tests;

/// <summary>
/// Integration tests tries to simulate the entire app environment with the dependencies active and running
/// </summary>
public abstract class IntegrationTest
{
    protected MemoryStream _localDatabaseStream;
    protected ILocalDatabase _localDatabase;
    protected MemoryStream _priceDatabaseStream;
    protected IPriceDatabase _priceDatabase;

    protected IServiceCollection _serviceCollection;
    protected IServiceProvider _serviceProvider;

    protected IntegrationTest()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    private void RegisterDependencies()
    {
        _serviceCollection = new ServiceCollection();
        
        _serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });

        var localStorageService = Substitute.For<ILocalStorageService>();
        localStorageService.LoadDataGridSettings().Returns(new DataGridSettings());
        localStorageService.LoadRecentFiles().Returns(new List<string>());
        localStorageService.LoadCulture().Returns("en-US");

        _serviceCollection
            .AddValtCore()
            .AddValtInfrastructure()
            .AddValtUI(localStorageService);

        //some extra dependencies for testing purposes
        _serviceCollection.AddSingleton<LivePricesUpdaterJob>();
        
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        //register the current service provider as the universal provider
        serviceProvider.SetAsContextScope();

        _serviceProvider = serviceProvider;
    }
    
    public void ReplaceService<T>(T implementation)
    {
        var descriptor = new ServiceDescriptor(typeof(T), implementation);
        
        var existing = _serviceCollection.FirstOrDefault(s => s.ServiceType == typeof(T));
        if (existing != null)
            _serviceCollection.Remove(existing);
            
        _serviceCollection.Add(descriptor);
        
        RebuildServiceProvider();
    }

    protected void RebuildServiceProvider()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        //register the current service provider as the universal provider
        serviceProvider.SetAsContextScope();

        _serviceProvider = serviceProvider;
    }

    [OneTimeSetUp]
    public async Task CreateTemporaryDb()
    {
        RegisterDependencies();
        
        _localDatabaseStream = new MemoryStream();

        _localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();
        _localDatabase.OpenInMemoryDatabase(_localDatabaseStream);
        
        ReplaceService(_localDatabase);
        
        _priceDatabaseStream = new MemoryStream();
        
        _priceDatabase = new PriceDatabase(new Clock(), Substitute.For<INotificationPublisher>());
        _priceDatabase.OpenInMemoryDatabase(_priceDatabaseStream);
        
        ReplaceService(_priceDatabase);
        
        await SeedDatabase();
    }

    [SetUp]
    public Task SetUp()
    {
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
        _localDatabase.Dispose();
        _priceDatabase.CloseDatabase();
        _priceDatabase.Dispose();
        await _localDatabaseStream.DisposeAsync();
        await _priceDatabaseStream.DisposeAsync();
    }
}