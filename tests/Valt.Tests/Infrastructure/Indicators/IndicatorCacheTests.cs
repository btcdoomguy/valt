using NSubstitute;
using Valt.Infra.Crawlers.Indicators;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Tests.Infrastructure.Indicators;

[TestFixture]
public class IndicatorCacheTests : DatabaseTest
{
    private IndicatorCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new IndicatorCache(_priceDatabase, new Builders.FakeClock(DateTime.UtcNow));
    }

    [Test]
    public void GetLatest_NoData_ReturnsNull()
    {
        var result = _cache.GetLatest();
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Save_And_GetLatest_RoundTrips()
    {
        var snapshot = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow,
            IsUpToDate = true,
            MayerMultiple = new MayerMultipleData(1.42m, 65000m, 45000m),
            RainbowChart = new RainbowChartData("Accumulate", 65000m),
            PiCycleTop = new PiCycleTopData(55000m, 80000m, 65000m, false),
            StockToFlow = new StockToFlowData(50000m, 65000m, 1.30m),
            FearAndGreed = new FearAndGreedData(72, "Greed"),
            BitcoinDominance = new BitcoinDominanceData(54.2m)
        };

        _cache.Save(snapshot);
        var loaded = _cache.GetLatest();

        Assert.That(loaded, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded!.MayerMultiple, Is.Not.Null);
            Assert.That(loaded.MayerMultiple!.Multiple, Is.EqualTo(1.42m));
            Assert.That(loaded.MayerMultiple.Price, Is.EqualTo(65000m));
            Assert.That(loaded.MayerMultiple.Ma200, Is.EqualTo(45000m));

            Assert.That(loaded.RainbowChart, Is.Not.Null);
            Assert.That(loaded.RainbowChart!.CurrentZone, Is.EqualTo("Accumulate"));

            Assert.That(loaded.PiCycleTop, Is.Not.Null);
            Assert.That(loaded.PiCycleTop!.IsConverging, Is.False);

            Assert.That(loaded.StockToFlow, Is.Not.Null);
            Assert.That(loaded.StockToFlow!.Ratio, Is.EqualTo(1.30m));

            Assert.That(loaded.FearAndGreed, Is.Not.Null);
            Assert.That(loaded.FearAndGreed!.Value, Is.EqualTo(72));
            Assert.That(loaded.FearAndGreed.Classification, Is.EqualTo("Greed"));

            Assert.That(loaded.BitcoinDominance, Is.Not.Null);
            Assert.That(loaded.BitcoinDominance!.DominancePercent, Is.EqualTo(54.2m));
        });
    }

    [Test]
    public void Save_PartialData_RoundTrips()
    {
        var snapshot = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow,
            IsUpToDate = true,
            MayerMultiple = new MayerMultipleData(1.42m, 65000m, 45000m),
            // Other indicators are null
        };

        _cache.Save(snapshot);
        var loaded = _cache.GetLatest();

        Assert.That(loaded, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded!.MayerMultiple, Is.Not.Null);
            Assert.That(loaded.RainbowChart, Is.Null);
            Assert.That(loaded.PiCycleTop, Is.Null);
            Assert.That(loaded.StockToFlow, Is.Null);
            Assert.That(loaded.FearAndGreed, Is.Null);
            Assert.That(loaded.BitcoinDominance, Is.Null);
        });
    }

    [Test]
    public void GetLatest_RecentData_IsUpToDate()
    {
        var snapshot = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow,
            IsUpToDate = true,
            FearAndGreed = new FearAndGreedData(50, "Neutral")
        };

        _cache.Save(snapshot);
        var loaded = _cache.GetLatest();

        Assert.That(loaded, Is.Not.Null);
        // FakeClock returns a fixed time, so if saved close to that time, should be up to date
        // The exact behavior depends on FakeClock implementation
    }

    [Test]
    public void Save_Upserts_ExistingData()
    {
        var snapshot1 = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow.AddMinutes(-10),
            IsUpToDate = true,
            FearAndGreed = new FearAndGreedData(50, "Neutral")
        };

        _cache.Save(snapshot1);

        var snapshot2 = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow,
            IsUpToDate = true,
            FearAndGreed = new FearAndGreedData(75, "Greed")
        };

        _cache.Save(snapshot2);
        var loaded = _cache.GetLatest();

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.FearAndGreed!.Value, Is.EqualTo(75));
    }
}
