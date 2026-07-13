using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Assets.PriceProviders;
using Valt.Infra.Modules.Assets.Services;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Assets;

[TestFixture]
public class AssetPriceUpdaterJobTests
{
    private AssetPriceUpdaterJob _job = null!;
    private ILocalDatabase _localDatabase = null!;
    private IAssetRepository _assetRepository = null!;
    private IAssetPriceProviderSelector _priceProviderSelector = null!;
    private INotificationPublisher _notificationPublisher = null!;
    private ILogger<AssetPriceUpdaterJob> _logger = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        _localDatabase = Substitute.For<ILocalDatabase>();
        _assetRepository = Substitute.For<IAssetRepository>();
        _priceProviderSelector = Substitute.For<IAssetPriceProviderSelector>();
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        _logger = Substitute.For<ILogger<AssetPriceUpdaterJob>>();

        _localDatabase.HasDatabaseOpen.Returns(true);

        _job = new AssetPriceUpdaterJob(
            _localDatabase,
            _assetRepository,
            _priceProviderSelector,
            _notificationPublisher,
            _logger);
    }

    [TearDown]
    public void TearDown()
    {
        (_localDatabase as IDisposable)?.Dispose();
        (_assetRepository as IDisposable)?.Dispose();
        (_priceProviderSelector as IDisposable)?.Dispose();
        (_notificationPublisher as IDisposable)?.Dispose();
        (_logger as IDisposable)?.Dispose();
    }

    [Test]
    public async Task RunAsync_SkipsSoldAssets()
    {
        var activeAsset = AssetBuilder.AnAsset()
            .WithName("AAPL Stock")
            .WithBasicDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.YahooFinance, 150m, "USD")
            .WithLastPriceUpdateAt(DateTime.UtcNow.AddMinutes(-10))
            .Build();

        var soldAsset = AssetBuilder.AnAsset()
            .WithName("MSFT Stock")
            .WithBasicDetails(AssetTypes.Stock, 5, "MSFT", AssetPriceSource.YahooFinance, 300m, "USD")
            .WithSold(true)
            .WithDateSold(new DateOnly(2025, 1, 15))
            .WithLastPriceUpdateAt(DateTime.UtcNow.AddMinutes(-10))
            .Build();

        _assetRepository.GetAllAsync().Returns(new[] { activeAsset, soldAsset });
        _priceProviderSelector.GetPriceAsync(Arg.Any<AssetPriceSource>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new AssetPriceResult(150m, "USD", DateTime.UtcNow));

        await _job.RunAsync(CancellationToken.None);

        await _priceProviderSelector.DidNotReceive()
            .GetPriceAsync(Arg.Any<AssetPriceSource>(), "MSFT", Arg.Any<string>());

        await _priceProviderSelector.Received()
            .GetPriceAsync(AssetPriceSource.YahooFinance, "AAPL", Arg.Any<string>());

        await _notificationPublisher.Received().PublishAsync(Arg.Any<AssetPricesUpdated>());
    }

    [Test]
    public async Task RunAsync_NoAssetsToUpdate_DoesNotCallProvider()
    {
        var soldAsset = AssetBuilder.AnAsset()
            .WithName("MSFT Stock")
            .WithBasicDetails(AssetTypes.Stock, 5, "MSFT", AssetPriceSource.YahooFinance, 300m, "USD")
            .WithSold(true)
            .WithDateSold(new DateOnly(2025, 1, 15))
            .WithLastPriceUpdateAt(DateTime.UtcNow.AddMinutes(-10))
            .Build();

        _assetRepository.GetAllAsync().Returns(new[] { soldAsset });

        await _job.RunAsync(CancellationToken.None);

        await _priceProviderSelector.DidNotReceive()
            .GetPriceAsync(Arg.Any<AssetPriceSource>(), Arg.Any<string>(), Arg.Any<string>());

        await _notificationPublisher.DidNotReceive().PublishAsync(Arg.Any<AssetPricesUpdated>());
    }
}
