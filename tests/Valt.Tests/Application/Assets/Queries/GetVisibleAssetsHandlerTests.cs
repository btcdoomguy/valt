using Valt.App.Modules.Assets.Queries.GetVisibleAssets;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetVisibleAssetsHandlerTests : DatabaseTest
{
    private GetVisibleAssetsHandler _handler = null!;

    protected override async Task SeedDatabase()
    {
        var visibleAsset1 = AssetBuilder.AStockAsset("AAPL", 150m, 10)
            .WithVisible(true)
            .Build();
        var visibleAsset2 = AssetBuilder.AnEtfAsset("SPY", 450m, 5)
            .WithVisible(true)
            .Build();
        var invisibleAsset = AssetBuilder.ACryptoAsset("ETH", 2500m, 2)
            .WithVisible(false)
            .Build();

        await _assetRepository.SaveAsync(visibleAsset1);
        await _assetRepository.SaveAsync(visibleAsset2);
        await _assetRepository.SaveAsync(invisibleAsset);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetVisibleAssetsHandler(_assetQueries);
    }

    [Test]
    public async Task HandleAsync_ReturnsOnlyVisibleAssets()
    {
        var query = new GetVisibleAssetsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.All(a => a.Visible), Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotReturnInvisibleAssets()
    {
        var query = new GetVisibleAssetsQuery();

        var result = await _handler.HandleAsync(query);

        var ethAsset = result.FirstOrDefault(a => a.Name == "ETH Crypto");
        Assert.That(ethAsset, Is.Null);
    }
}
