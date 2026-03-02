using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.Indicators;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Tests.Infrastructure.Indicators;

[TestFixture]
public class IndicatorsUpdaterJobTests
{
    private IBitcoinComIndicatorsProvider _bitcoinComProvider = null!;
    private IFearAndGreedProvider _fearAndGreedProvider = null!;
    private IBitcoinDominanceProvider _dominanceProvider = null!;
    private IIndicatorCache _indicatorCache = null!;
    private IPriceDatabase _priceDatabase = null!;
    private IClock _clock = null!;
    private INotificationPublisher _notificationPublisher = null!;
    private ILogger<IndicatorsUpdaterJob> _logger = null!;
    private IndicatorsUpdaterJob _job = null!;

    [TearDown]
    public void TearDown()
    {
        _priceDatabase.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _bitcoinComProvider = Substitute.For<IBitcoinComIndicatorsProvider>();
        _fearAndGreedProvider = Substitute.For<IFearAndGreedProvider>();
        _dominanceProvider = Substitute.For<IBitcoinDominanceProvider>();
        _indicatorCache = Substitute.For<IIndicatorCache>();
        _priceDatabase = Substitute.For<IPriceDatabase>();
        _clock = Substitute.For<IClock>();
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        _logger = Substitute.For<ILogger<IndicatorsUpdaterJob>>();

        _priceDatabase.HasDatabaseOpen.Returns(true);
        _clock.GetCurrentDateTimeUtc().Returns(new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc));

        _job = new IndicatorsUpdaterJob(
            _bitcoinComProvider,
            _fearAndGreedProvider,
            _dominanceProvider,
            _indicatorCache,
            _priceDatabase,
            _clock,
            _notificationPublisher,
            _logger);
    }

    [Test]
    public void Job_HasCorrectProperties()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_job.SystemName, Is.EqualTo(BackgroundJobSystemNames.IndicatorsUpdater));
            Assert.That(_job.JobType, Is.EqualTo(BackgroundJobTypes.PriceDatabase));
            Assert.That(_job.Interval, Is.EqualTo(TimeSpan.FromSeconds(120)));
        });
    }

    [Test]
    public async Task RunAsync_AllProvidersSucceed_SavesCompleteSnapshot()
    {
        SetupAllProvidersSuccess();

        await _job.RunAsync(CancellationToken.None);

        _indicatorCache.Received(1).Save(Arg.Is<IndicatorSnapshot>(s =>
            s.MayerMultiple != null &&
            s.RainbowChart != null &&
            s.FearAndGreed != null &&
            s.BitcoinDominance != null &&
            s.IsUpToDate));

        await _notificationPublisher.Received(1).PublishAsync(Arg.Any<IndicatorsUpdatedMessage>());
    }

    [Test]
    public async Task RunAsync_OneProviderFails_UsesCachedValueForFailedProvider()
    {
        // Set up all providers to succeed
        SetupAllProvidersSuccess();

        // But make Fear & Greed fail
        _fearAndGreedProvider.GetAsync().ThrowsAsync(new HttpRequestException("API down"));

        // Set up cached value for Fear & Greed
        var cachedSnapshot = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow.AddMinutes(-5),
            FearAndGreed = new FearAndGreedData(50, "Neutral")
        };
        _indicatorCache.GetLatest().Returns(cachedSnapshot);

        await _job.RunAsync(CancellationToken.None);

        _indicatorCache.Received(1).Save(Arg.Is<IndicatorSnapshot>(s =>
            s.MayerMultiple != null &&
            s.FearAndGreed != null && // Should use cached value
            s.FearAndGreed.Value == 50));
    }

    [Test]
    public async Task RunAsync_AllProvidersFail_UsesAllCachedValues()
    {
        _bitcoinComProvider.GetMayerMultipleAsync().ThrowsAsync(new HttpRequestException("offline"));
        _bitcoinComProvider.GetRainbowChartAsync().ThrowsAsync(new HttpRequestException("offline"));
        _fearAndGreedProvider.GetAsync().ThrowsAsync(new HttpRequestException("offline"));
        _dominanceProvider.GetAsync().ThrowsAsync(new HttpRequestException("offline"));

        var cachedSnapshot = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow.AddMinutes(-10),
            MayerMultiple = new MayerMultipleData(1.5m, 70000m, 46000m),
            FearAndGreed = new FearAndGreedData(60, "Greed")
        };
        _indicatorCache.GetLatest().Returns(cachedSnapshot);

        await _job.RunAsync(CancellationToken.None);

        _indicatorCache.Received(1).Save(Arg.Is<IndicatorSnapshot>(s =>
            s.MayerMultiple != null &&
            s.MayerMultiple.Multiple == 1.5m &&
            s.FearAndGreed != null &&
            s.FearAndGreed.Value == 60));
    }

    [Test]
    public async Task RunAsync_DatabaseNotOpen_SkipsUpdate()
    {
        _priceDatabase.HasDatabaseOpen.Returns(false);

        await _job.RunAsync(CancellationToken.None);

        _indicatorCache.DidNotReceive().Save(Arg.Any<IndicatorSnapshot>());
        await _notificationPublisher.DidNotReceive().PublishAsync(Arg.Any<IndicatorsUpdatedMessage>());
    }

    [Test]
    public async Task StartAsync_WithCachedData_PublishesImmediately()
    {
        var cachedSnapshot = new IndicatorSnapshot
        {
            LastUpdatedUtc = DateTime.UtcNow.AddMinutes(-5),
            FearAndGreed = new FearAndGreedData(72, "Greed")
        };
        _indicatorCache.GetLatest().Returns(cachedSnapshot);

        await _job.StartAsync(CancellationToken.None);

        await _notificationPublisher.Received(1).PublishAsync(
            Arg.Is<IndicatorsUpdatedMessage>(m => m.Snapshot == cachedSnapshot));
    }

    [Test]
    public async Task StartAsync_NoCachedData_DoesNotPublish()
    {
        _indicatorCache.GetLatest().Returns((IndicatorSnapshot?)null);

        await _job.StartAsync(CancellationToken.None);

        await _notificationPublisher.DidNotReceive().PublishAsync(Arg.Any<IndicatorsUpdatedMessage>());
    }

    [Test]
    public async Task RunAsync_PartialProviderFailure_StillSavesAvailableData()
    {
        // Only Mayer Multiple succeeds
        _bitcoinComProvider.GetMayerMultipleAsync()
            .Returns(Task.FromResult(new MayerMultipleData(1.42m, 65000m, 45000m)));
        _bitcoinComProvider.GetRainbowChartAsync().ThrowsAsync(new Exception("fail"));
        _fearAndGreedProvider.GetAsync().ThrowsAsync(new Exception("fail"));
        _dominanceProvider.GetAsync().ThrowsAsync(new Exception("fail"));

        _indicatorCache.GetLatest().Returns((IndicatorSnapshot?)null);

        await _job.RunAsync(CancellationToken.None);

        _indicatorCache.Received(1).Save(Arg.Is<IndicatorSnapshot>(s =>
            s.MayerMultiple != null &&
            s.MayerMultiple.Multiple == 1.42m &&
            s.RainbowChart == null));
    }

    private void SetupAllProvidersSuccess()
    {
        _bitcoinComProvider.GetMayerMultipleAsync()
            .Returns(Task.FromResult(new MayerMultipleData(1.42m, 65000m, 45000m)));
        _bitcoinComProvider.GetRainbowChartAsync()
            .Returns(Task.FromResult(new RainbowChartData("Accumulate", 65000m)));
        _fearAndGreedProvider.GetAsync()
            .Returns(Task.FromResult(new FearAndGreedData(72, "Greed")));
        _dominanceProvider.GetAsync()
            .Returns(Task.FromResult(new BitcoinDominanceData(54.2m)));
    }
}
