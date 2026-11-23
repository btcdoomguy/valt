using System.Reflection;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Valt.App;
using Valt.Core;
using Valt.Infra;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Mcp.Server;
using Valt.Infra.Modules.Assets.PriceProviders;
using Valt.Infra.TransactionTerms;

namespace Valt.Tests.Infrastructure;

[TestFixture]
public class CorrectnessRegressionTests : DatabaseTest
{
    [Test]
    public void TransactionTermService_RemoveEntry_PersistsDecrementedCountAndDeletesAtZero()
    {
        var service = new TransactionTermService(_localDatabase);
        var categoryId = ObjectId.NewObjectId().ToString();

        service.AddEntry("Coffee", categoryId, 1000L, 5m);
        service.AddEntry("Coffee", categoryId, 1000L, 5m);

        var entryAfterTwoAdds = _localDatabase.GetTransactionTerms()
            .FindOne(x => x.Name == "Coffee" && x.CategoryId == new ObjectId(categoryId));
        Assert.That(entryAfterTwoAdds, Is.Not.Null);
        Assert.That(entryAfterTwoAdds!.Count, Is.EqualTo(2));

        // First RemoveEntry should decrement Count to 1 and persist to LiteDB
        service.RemoveEntry("Coffee", categoryId);

        var entryAfterOneRemove = _localDatabase.GetTransactionTerms()
            .FindOne(x => x.Name == "Coffee" && x.CategoryId == new ObjectId(categoryId));
        Assert.That(entryAfterOneRemove, Is.Not.Null);
        Assert.That(entryAfterOneRemove!.Count, Is.EqualTo(1));

        // Second RemoveEntry should decrement Count to 0 and delete the record
        service.RemoveEntry("Coffee", categoryId);

        var entryAfterSecondRemove = _localDatabase.GetTransactionTerms()
            .FindOne(x => x.Name == "Coffee" && x.CategoryId == new ObjectId(categoryId));
        Assert.That(entryAfterSecondRemove, Is.Null);
    }

    [Test]
    public void AddValtInfrastructure_SharesSingleTransactionTermServiceInstanceAndMcpForwardsAssetPriceProviderSelector()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValtCore();
        services.AddValtApp();
        services.AddValtInfrastructure();

        using var provider = services.BuildServiceProvider();

        var interfaceInstance = provider.GetRequiredService<ITransactionTermService>();
        var concreteInstance = provider.GetRequiredService<TransactionTermService>();

        Assert.That(ReferenceEquals(interfaceInstance, concreteInstance), Is.True);

        var mcpServerService = provider.GetRequiredService<McpServerService>();
        var forwardMethod = typeof(McpServerService).GetMethod(
            "ForwardServicesFromMainApp",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(forwardMethod, Is.Not.Null);

        var mcpServices = new ServiceCollection();
        forwardMethod!.Invoke(mcpServerService, [mcpServices]);

        using var mcpProvider = mcpServices.BuildServiceProvider();
        var selector = mcpProvider.GetService<IAssetPriceProviderSelector>();
        Assert.That(selector, Is.Not.Null);
        Assert.That(
            ReferenceEquals(selector, provider.GetRequiredService<IAssetPriceProviderSelector>()),
            Is.True);
    }

    [Test]
    public async Task CoinGeckoRateLimiter_WaitAsync_ReservesDistinctTimeSlotsForConcurrentCallers()
    {
        var limiter = new CoinGeckoRateLimiter(NullLogger<CoinGeckoRateLimiter>.Instance);
        var minIntervalField = typeof(CoinGeckoRateLimiter).GetField(
            "_minInterval",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(minIntervalField, Is.Not.Null);
        minIntervalField!.SetValue(limiter, TimeSpan.FromMilliseconds(150));

        // Initial call completes immediately and sets the first slot
        await limiter.WaitAsync();

        using var barrier = new SemaphoreSlim(0, 3);
        var completionTimes = new DateTime[3];

        var tasks = Enumerable.Range(0, 3).Select(async index =>
        {
            await barrier.WaitAsync();
            await limiter.WaitAsync();
            completionTimes[index] = DateTime.UtcNow;
        }).ToArray();

        barrier.Release(3);
        await Task.WhenAll(tasks);

        var ordered = completionTimes.OrderBy(t => t).ToArray();
        var gap1 = ordered[1] - ordered[0];
        var gap2 = ordered[2] - ordered[1];

        Assert.Multiple(() =>
        {
            Assert.That(gap1, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100)),
                $"Expected second concurrent caller to be delayed after first caller, actual gap: {gap1.TotalMilliseconds}ms");
            Assert.That(gap2, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100)),
                $"Expected third concurrent caller to be delayed after second caller, actual gap: {gap2.TotalMilliseconds}ms");
        });
    }
}
