using Valt.App.Modules.Assets.Queries.GetSoldAssets;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetSoldAssetsHandlerTests : DatabaseTest
{
    private GetSoldAssetsHandler _handler = null!;

    protected override async Task SeedDatabase()
    {
        var activeAsset = AssetBuilder.AStockAsset("AAPL", 150m, 10).Build();
        var soldAsset1 = AssetBuilder.ACryptoAsset("SOL", 2500m, 5)
            .WithSold(true)
            .WithDateSold(new DateOnly(2025, 1, 20))
            .Build();
        var soldAsset2 = AssetBuilder.AnEtfAsset("SPY", 450m, 5)
            .WithSold(true)
            .WithDateSold(new DateOnly(2025, 1, 10))
            .Build();

        await _assetRepository.SaveAsync(activeAsset);
        await _assetRepository.SaveAsync(soldAsset1);
        await _assetRepository.SaveAsync(soldAsset2);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetSoldAssetsHandler(_assetQueries);
    }

    [Test]
    public async Task HandleAsync_ReturnsOnlySoldAssets()
    {
        var query = new GetSoldAssetsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(a => a.Name == "AAPL Stock"), Is.False, "Active asset should not be in sold list");
            Assert.That(result.All(a => a.IsSold), Is.True, "All returned assets should be sold");
        });
    }

    [Test]
    public async Task HandleAsync_SortsByDateSoldDescendingThenByName()
    {
        var query = new GetSoldAssetsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result.Select(a => a.Name), Is.EqualTo(new[] { "SOL Crypto", "SPY ETF" }));
    }
}
