using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valt.Core;
using Valt.Core.Kernel.Factories;
using Valt.Infra;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.UI;

namespace Valt.Tests;

/// <summary>
/// Integration tests tries to simulate the entire app environment with the dependencies active and running
/// </summary>
public abstract class IntegrationTest
{
    private MemoryStream _stream;
    protected ILocalDatabase _localDatabase;
    protected IServiceProvider _serviceProvider;

    protected IntegrationTest()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    private void RegisterDependencies()
    {
        var collection = new ServiceCollection();
        
        collection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });

        collection
            .AddValtCore()
            .AddValtInfrastructure()
            .AddValtUI();
        
        var serviceProvider = collection.BuildServiceProvider();

        //register the current service provider as the universal provider
        serviceProvider.SetAsContextScope();

        _serviceProvider = serviceProvider;
    }

    [OneTimeSetUp]
    public async Task CreateTemporaryDb()
    {
        RegisterDependencies();
        
        _stream = new MemoryStream();

        _localDatabase = _serviceProvider.GetRequiredService<ILocalDatabase>();
        _localDatabase.OpenInMemoryDatabase(_stream);
        
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
        await _stream.DisposeAsync();
    }
}