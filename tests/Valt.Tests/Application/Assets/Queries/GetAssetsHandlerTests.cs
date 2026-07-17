using Valt.App.Modules.Assets.Queries.GetAssets;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetAssetsHandlerTests : DatabaseTest
{
    private GetAssetsHandler _handler = null!;

    protected override async Task SeedDatabase()
    {
        var asset1 = AssetBuilder.AStockAsset("AAPL", 150m, 10).Build();
        var asset2 = AssetBuilder.AnEtfAsset("SPY", 450m, 5).Build();
        var asset3 = AssetBuilder.ACryptoAsset("ETH", 2500m, 2)
            .WithVisible(false)
            .Build();
        var soldAsset = AssetBuilder.ACryptoAsset("SOL", 150m, 5)
            .WithSold(true)
            .WithDateSold(new DateOnly(2025, 1, 15))
            .Build();

        await _assetRepository.SaveAsync(asset1);
        await _assetRepository.SaveAsync(asset2);
        await _assetRepository.SaveAsync(asset3);
        await _assetRepository.SaveAsync(soldAsset);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetAssetsHandler(_assetQueries);
    }

    [Test]
    public async Task HandleAsync_ReturnsActiveAssets()
    {
        var query = new GetAssetsQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Any(a => a.Name == "SOL Crypto"), Is.False, "Sold asset should not be in active list");
    }

    [Test]
    public async Task HandleAsync_ReturnsAssetDetails()
    {
        var query = new GetAssetsQuery();

        var result = await _handler.HandleAsync(query);

        var appleAsset = result.FirstOrDefault(a => a.Name == "AAPL Stock");
        Assert.Multiple(() =>
        {
            Assert.That(appleAsset, Is.Not.Null);
            Assert.That(appleAsset!.Symbol, Is.EqualTo("AAPL"));
            Assert.That(appleAsset.CurrentPrice, Is.EqualTo(150m));
            Assert.That(appleAsset.Quantity, Is.EqualTo(10m));
            Assert.That(appleAsset.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(appleAsset.Visible, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsInvisibleAssets()
    {
        var query = new GetAssetsQuery();

        var result = await _handler.HandleAsync(query);

        var invisibleAsset = result.FirstOrDefault(a => a.Visible == false);
        Assert.That(invisibleAsset, Is.Not.Null);
        Assert.That(invisibleAsset!.Name, Is.EqualTo("ETH Crypto"));
    }
}
