using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Assets;
using Valt.Infra.Modules.Assets.Services;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Assets;

[TestFixture]
public class WealthSnapshotJobTests
{
    private ILocalDatabase _localDatabase = null!;
    private IWealthSnapshotRepository _wealthSnapshotRepository = null!;
    private IAssetQueries _assetQueries = null!;
    private IPriceDatabaseRatesProvider _ratesProvider = null!;
    private IClock _clock = null!;
    private ILogger<WealthSnapshotJob> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _localDatabase = Substitute.For<ILocalDatabase>();
        _wealthSnapshotRepository = Substitute.For<IWealthSnapshotRepository>();
        _assetQueries = Substitute.For<IAssetQueries>();
        _ratesProvider = Substitute.For<IPriceDatabaseRatesProvider>();
        _clock = new FakeClock(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc));
        _logger = Substitute.For<ILogger<WealthSnapshotJob>>();

        _localDatabase.HasDatabaseOpen.Returns(true);

        _ratesProvider.GetLatestRatesAsync(Arg.Any<CancellationToken>())
            .Returns(BuildRatesMessage());
        _assetQueries.GetSummaryAsync(Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<IReadOnlyDictionary<string, decimal>?>())
            .Returns(BuildSummary(123.45m, 246900L));

        _job = BuildJob(_localDatabase, _wealthSnapshotRepository);
    }

    private WealthSnapshotJob _job = null!;

    [TearDown]
    public void TearDown()
    {
        (_localDatabase as IDisposable)?.Dispose();
        (_wealthSnapshotRepository as IDisposable)?.Dispose();
        (_assetQueries as IDisposable)?.Dispose();
        (_ratesProvider as IDisposable)?.Dispose();
        (_logger as IDisposable)?.Dispose();
    }

    [Test]
    public async Task RunAsync_When_Database_Not_Open_Should_Not_Write()
    {
        _localDatabase.HasDatabaseOpen.Returns(false);

        await _job.RunAsync(CancellationToken.None);

        await _assetQueries.DidNotReceive()
            .GetSummaryAsync(Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<IReadOnlyDictionary<string, decimal>?>());
        await _wealthSnapshotRepository.DidNotReceive().SaveAsync(Arg.Any<WealthSnapshotEntity>());
    }

    [Test]
    public async Task RunAsync_When_Rates_Present_And_No_Row_Today_Should_Write_Snapshot_With_Utc_Datetime()
    {
        await _job.RunAsync(CancellationToken.None);

        await _wealthSnapshotRepository.Received(1).SaveAsync(Arg.Is<WealthSnapshotEntity>(x =>
            x.SnapshotDay == new DateOnly(2024, 1, 15) &&
            x.SnapshotDateTimeUtc == new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc) &&
            x.TotalValueInMainCurrency == 123.45m &&
            x.TotalValueInSats == 246900L &&
            x.MainCurrencyCode == FiatCurrency.Usd.Code));
    }

    [Test]
    public async Task RunAsync_Twice_Same_Day_Should_Leave_Exactly_One_Row_With_Second_Value_Winning()
    {
        await using var stream = new MemoryStream();
        var realDatabase = new LocalDatabase(new Clock());
        realDatabase.OpenInMemoryDatabase(stream);
        var realRepository = new WealthSnapshotRepository(realDatabase);
        var job = BuildJob(realDatabase, realRepository);

        try
        {
            await job.RunAsync(CancellationToken.None);

            _assetQueries.GetSummaryAsync(Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<IReadOnlyDictionary<string, decimal>?>())
                .Returns(BuildSummary(500m, 1000000L));
            await job.RunAsync(CancellationToken.None);

            var rows = realDatabase.GetWealthSnapshots().FindAll().ToList();
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].TotalValueInMainCurrency, Is.EqualTo(500m));
            Assert.That(rows[0].TotalValueInSats, Is.EqualTo(1000000L));
        }
        finally
        {
            realDatabase.Dispose();
            await stream.DisposeAsync();
        }
    }

    private WealthSnapshotJob BuildJob(ILocalDatabase localDatabase, IWealthSnapshotRepository repository)
    {
        return new WealthSnapshotJob(
            localDatabase,
            repository,
            _assetQueries,
            _ratesProvider,
            _clock,
            _logger);
    }

    private static LivePriceUpdateMessage BuildRatesMessage()
    {
        return new LivePriceUpdateMessage(
            new BtcPrice(DateTime.UtcNow, true, new[] { new BtcPrice.Item(FiatCurrency.Usd.Code, 50000m) }),
            new FiatUsdPrice(DateTime.UtcNow, true, new[] { new FiatUsdPrice.Item(FiatCurrency.Usd, 1m) }),
            IsUpToDate: true);
    }

    private static AssetSummaryDTO BuildSummary(decimal totalFiat, long totalSats)
    {
        return new AssetSummaryDTO
        {
            TotalAssets = 1,
            VisibleAssets = 1,
            AssetsIncludedInNetWorth = 1,
            ValuesByCurrency = Array.Empty<AssetValueByCurrencyDTO>(),
            TotalAssetsValueInMainCurrency = totalFiat,
            TotalLiabilitiesInMainCurrency = 0m,
            LiabilitiesCount = 0,
            TotalValueInMainCurrency = totalFiat,
            TotalValueInSats = totalSats
        };
    }
}
