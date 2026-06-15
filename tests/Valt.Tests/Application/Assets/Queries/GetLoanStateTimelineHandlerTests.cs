using Valt.App.Modules.Assets.Queries.GetLoanStateTimeline;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetLoanStateTimelineHandlerTests : DatabaseTest
{
    private GetLoanStateTimelineHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetLoanStateTimelineHandler(_assetRepository);
    }

    [TearDown]
    public async Task ClearAssets()
    {
        var existing = await _assetRepository.GetAllAsync();
        foreach (var asset in existing)
            await _assetRepository.DeleteAsync(asset);
    }

    [Test]
    public async Task HandleAsync_WithOutOfOrderSnapshots_ReturnsChronologicalOrder()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(new DateOnly(2025, 6, 1), 26_000m)
            .WithSnapshot(new DateOnly(2025, 3, 1), 25_500m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var result = await _handler.HandleAsync(new GetLoanStateTimelineQuery { AssetId = asset.Id.Value });

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 3, 1)));
            Assert.That(result[0].CurrentTotalDebt, Is.EqualTo(25_500m));
            Assert.That(result[1].EffectiveDate, Is.EqualTo(new DateOnly(2025, 6, 1)));
            Assert.That(result[1].CurrentTotalDebt, Is.EqualTo(26_000m));
        });
    }

    [Test]
    public async Task HandleAsync_NonExistentAsset_ReturnsEmptyList()
    {
        var result = await _handler.HandleAsync(new GetLoanStateTimelineQuery
        {
            AssetId = "000000000000000000000000"
        });

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_StockAsset_ReturnsEmptyList()
    {
        var asset = AssetBuilder.AStockAsset().Build();
        await _assetRepository.SaveAsync(asset);

        var result = await _handler.HandleAsync(new GetLoanStateTimelineQuery { AssetId = asset.Id.Value });

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_MapsAllSnapshotFields()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(
                effectiveDate: new DateOnly(2025, 3, 1),
                currentTotalDebt: 25_500m,
                note: "Q1 update")
            .Build();
        await _assetRepository.SaveAsync(asset);

        var result = await _handler.HandleAsync(new GetLoanStateTimelineQuery { AssetId = asset.Id.Value });

        var dto = result.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dto.PlatformName, Is.EqualTo("HodlHodl"));
            Assert.That(dto.CollateralSats, Is.EqualTo(100_000_000L));
            Assert.That(dto.LoanAmount, Is.EqualTo(25_000m));
            Assert.That(dto.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(dto.Apr, Is.EqualTo(0.12m));
            Assert.That(dto.InitialLtv, Is.EqualTo(50m));
            Assert.That(dto.LiquidationLtv, Is.EqualTo(80m));
            Assert.That(dto.MarginCallLtv, Is.EqualTo(70m));
            Assert.That(dto.Fees, Is.EqualTo(100m));
            Assert.That(dto.LoanStartDate, Is.EqualTo(new DateOnly(2025, 1, 1)));
            Assert.That(dto.RepaymentDate, Is.EqualTo(new DateOnly(2026, 1, 1)));
            Assert.That(dto.StatusId, Is.EqualTo((int)LoanStatus.Active));
            Assert.That(dto.CurrentBtcPriceInLoanCurrency, Is.EqualTo(50_000m));
            Assert.That(dto.FixedTotalDebt, Is.Null);
            Assert.That(dto.CurrentTotalDebt, Is.EqualTo(25_500m));
            Assert.That(dto.EffectiveDate, Is.EqualTo(new DateOnly(2025, 3, 1)));
            Assert.That(dto.Note, Is.EqualTo("Q1 update"));
        });
    }
}
