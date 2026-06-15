using Valt.App.Modules.Assets.Queries.GetLatestLoanState;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetLatestLoanStateHandlerTests : DatabaseTest
{
    private GetLatestLoanStateHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetLatestLoanStateHandler(_assetRepository);
    }

    [TearDown]
    public async Task ClearAssets()
    {
        var existing = await _assetRepository.GetAllAsync();
        foreach (var asset in existing)
            await _assetRepository.DeleteAsync(asset);
    }

    [Test]
    public async Task HandleAsync_WithMultipleSnapshots_ReturnsLatestFieldsAndAssetMetadata()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithName("My BTC Loan")
            .WithSnapshot(new DateOnly(2025, 3, 1), 25_500m)
            .WithSnapshot(
                effectiveDate: new DateOnly(2025, 6, 1),
                currentTotalDebt: 26_000m,
                collateralSats: 110_000_000,
                apr: 0.13m,
                fees: 150m,
                note: "Latest update")
            .Build();
        await _assetRepository.SaveAsync(asset);

        var result = await _handler.HandleAsync(new GetLatestLoanStateQuery { AssetId = asset.Id.Value });

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.AssetId, Is.EqualTo(asset.Id.Value));
            Assert.That(result.AssetName, Is.EqualTo("My BTC Loan"));
            Assert.That(result.CurrentTotalDebt, Is.EqualTo(26_000m));
            Assert.That(result.CollateralSats, Is.EqualTo(110_000_000L));
            Assert.That(result.Apr, Is.EqualTo(0.13m));
            Assert.That(result.Fees, Is.EqualTo(150m));
            Assert.That(result.EffectiveDate, Is.EqualTo(new DateOnly(2025, 6, 1)));
            Assert.That(result.Note, Is.EqualTo("Latest update"));
        });
    }

    [Test]
    public async Task HandleAsync_NonExistentAsset_ReturnsNull()
    {
        var result = await _handler.HandleAsync(new GetLatestLoanStateQuery
        {
            AssetId = "000000000000000000000000"
        });

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_StockAsset_ReturnsNull()
    {
        var asset = AssetBuilder.AStockAsset().Build();
        await _assetRepository.SaveAsync(asset);

        var result = await _handler.HandleAsync(new GetLatestLoanStateQuery { AssetId = asset.Id.Value });

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_BtcLendingAsset_ReturnsNull()
    {
        var asset = AssetBuilder.ABtcLending().Build();
        await _assetRepository.SaveAsync(asset);

        var result = await _handler.HandleAsync(new GetLatestLoanStateQuery { AssetId = asset.Id.Value });

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_MapsAllLatestSnapshotFields()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(
                effectiveDate: new DateOnly(2025, 6, 1),
                currentTotalDebt: 26_000m,
                note: "Mid-year")
            .Build();
        await _assetRepository.SaveAsync(asset);

        var result = await _handler.HandleAsync(new GetLatestLoanStateQuery { AssetId = asset.Id.Value });

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.PlatformName, Is.EqualTo("HodlHodl"));
            Assert.That(result.LoanAmount, Is.EqualTo(25_000m));
            Assert.That(result.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(result.InitialLtv, Is.EqualTo(50m));
            Assert.That(result.LiquidationLtv, Is.EqualTo(80m));
            Assert.That(result.MarginCallLtv, Is.EqualTo(70m));
            Assert.That(result.LoanStartDate, Is.EqualTo(new DateOnly(2025, 1, 1)));
            Assert.That(result.RepaymentDate, Is.EqualTo(new DateOnly(2026, 1, 1)));
            Assert.That(result.StatusId, Is.EqualTo((int)LoanStatus.Active));
            Assert.That(result.CurrentBtcPriceInLoanCurrency, Is.EqualTo(50_000m));
            Assert.That(result.FixedTotalDebt, Is.Null);
        });
    }
}
