using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Core.Modules.Assets.Events;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Assets;

[TestFixture]
public class AssetRepositoryTests
{
    private MemoryStream _localDatabaseStream;
    private ILocalDatabase _localDatabase;
    private IDomainEventPublisher _domainEventPublisher;
    private AssetRepository _repository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());

        _localDatabaseStream = new MemoryStream();
        _localDatabase = new LocalDatabase(new Clock());
        _localDatabase.OpenInMemoryDatabase(_localDatabaseStream);
    }

    [SetUp]
    public void SetUp()
    {
        _domainEventPublisher = Substitute.For<IDomainEventPublisher>();
        _repository = new AssetRepository(_localDatabase, _domainEventPublisher);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _localDatabase.CloseDatabase();
        _localDatabase.Dispose();
        await _localDatabaseStream.DisposeAsync();
    }

    #region SaveAsync Tests

    [Test]
    public async Task SaveAsync_Should_Store_And_Retrieve_New_Asset()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("Test Stock"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD"),
            Icon.Empty);

        // Act
        await _repository.SaveAsync(asset);

        // Assert
        var retrievedAsset = await _repository.GetByIdAsync(asset.Id);
        Assert.That(retrievedAsset, Is.Not.Null);
        Assert.That(retrievedAsset!.Id, Is.EqualTo(asset.Id));
        Assert.That(retrievedAsset.Name.Value, Is.EqualTo("Test Stock"));
        Assert.That(retrievedAsset.GetCurrentPrice(), Is.EqualTo(150m));
    }

    [Test]
    public async Task SaveAsync_Should_Clear_Events_After_Saving()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("Event Test Asset"),
            new BasicAssetDetails(AssetTypes.Stock, 5, "MSFT", AssetPriceSource.Manual, 400m, "USD"),
            Icon.Empty);

        Assert.That(asset.Events.Count, Is.GreaterThan(0));

        // Act
        await _repository.SaveAsync(asset);

        // Assert
        Assert.That(asset.Events, Is.Empty);
    }

    [Test]
    public async Task SaveAsync_Should_Publish_Domain_Events()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("Publish Test Asset"),
            new BasicAssetDetails(AssetTypes.Etf, 20, "SPY", AssetPriceSource.Manual, 450m, "USD"),
            Icon.Empty);

        // Act
        await _repository.SaveAsync(asset);

        // Assert
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AssetCreatedEvent>());
    }

    [Test]
    public async Task SaveAsync_Should_Store_BasicAssetDetails()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("Basic Asset"),
            new BasicAssetDetails(AssetTypes.Crypto, 2.5m, "ETH", AssetPriceSource.YahooFinance, 2500m, "USD"),
            Icon.Empty);

        // Act
        await _repository.SaveAsync(asset);

        // Assert
        var retrieved = await _repository.GetByIdAsync(asset.Id);
        Assert.That(retrieved, Is.Not.Null);
        var details = retrieved!.Details as BasicAssetDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.AssetType, Is.EqualTo(AssetTypes.Crypto));
        Assert.That(details.Quantity, Is.EqualTo(2.5m));
        Assert.That(details.Symbol, Is.EqualTo("ETH"));
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.YahooFinance));
    }

    [Test]
    public async Task SaveAsync_Should_Store_RealEstateAssetDetails()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("My House"),
            new RealEstateAssetDetails(500000m, "USD", "123 Main St", 2500m),
            Icon.Empty);

        // Act
        await _repository.SaveAsync(asset);

        // Assert
        var retrieved = await _repository.GetByIdAsync(asset.Id);
        Assert.That(retrieved, Is.Not.Null);
        var details = retrieved!.Details as RealEstateAssetDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.CurrentValue, Is.EqualTo(500000m));
        Assert.That(details.Address, Is.EqualTo("123 Main St"));
        Assert.That(details.MonthlyRentalIncome, Is.EqualTo(2500m));
    }

    [Test]
    public async Task SaveAsync_Should_Store_LeveragedPositionDetails()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("BTC Long 10x"),
            new LeveragedPositionDetails(
                1000m, 50000m, 10m, 45000m, 55000m, "USD", "BTC", AssetPriceSource.LivePrice, true),
            Icon.Empty);

        // Act
        await _repository.SaveAsync(asset);

        // Assert
        var retrieved = await _repository.GetByIdAsync(asset.Id);
        Assert.That(retrieved, Is.Not.Null);
        var details = retrieved!.Details as LeveragedPositionDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.Collateral, Is.EqualTo(1000m));
        Assert.That(details.EntryPrice, Is.EqualTo(50000m));
        Assert.That(details.Leverage, Is.EqualTo(10m));
        Assert.That(details.IsLong, Is.True);
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.LivePrice));
    }

    [Test]
    public async Task SaveAsync_Should_Update_Existing_Asset()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("Update Test Asset"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "GOOGL", AssetPriceSource.Manual, 100m, "USD"),
            Icon.Empty);

        await _repository.SaveAsync(asset);

        // Act - Update the price
        asset.UpdatePrice(150m);
        await _repository.SaveAsync(asset);

        // Assert
        var retrieved = await _repository.GetByIdAsync(asset.Id);
        Assert.That(retrieved!.GetCurrentPrice(), Is.EqualTo(150m));
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public async Task GetAllAsync_Should_Return_All_Assets()
    {
        // Arrange
        var asset1 = Asset.New(
            new AssetName("GetAll Asset 1"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "NVDA", AssetPriceSource.Manual, 800m, "USD"),
            Icon.Empty);

        var asset2 = Asset.New(
            new AssetName("GetAll Asset 2"),
            new BasicAssetDetails(AssetTypes.Etf, 5, "QQQ", AssetPriceSource.Manual, 400m, "USD"),
            Icon.Empty);

        await _repository.SaveAsync(asset1);
        await _repository.SaveAsync(asset2);

        // Act
        var assets = (await _repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(assets.Any(a => a.Id == asset1.Id), Is.True);
        Assert.That(assets.Any(a => a.Id == asset2.Id), Is.True);
    }

    #endregion

    #region GetVisibleAsync Tests

    [Test]
    public async Task GetVisibleAsync_Should_Return_Only_Visible_Assets()
    {
        // Arrange
        var visibleAsset = Asset.New(
            new AssetName("Visible Asset"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "V1", AssetPriceSource.Manual, 100m, "USD"),
            Icon.Empty,
            includeInNetWorth: true,
            visible: true);

        var hiddenAsset = Asset.New(
            new AssetName("Hidden Asset"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "H1", AssetPriceSource.Manual, 100m, "USD"),
            Icon.Empty,
            includeInNetWorth: true,
            visible: false);

        await _repository.SaveAsync(visibleAsset);
        await _repository.SaveAsync(hiddenAsset);

        // Act
        var visibleAssets = (await _repository.GetVisibleAsync()).ToList();

        // Assert
        Assert.That(visibleAssets.Any(a => a.Id == visibleAsset.Id), Is.True);
        Assert.That(visibleAssets.Any(a => a.Id == hiddenAsset.Id), Is.False);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_Should_Remove_Asset()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("Delete Test Asset"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "DEL", AssetPriceSource.Manual, 100m, "USD"),
            Icon.Empty);

        await _repository.SaveAsync(asset);

        // Verify asset exists
        var existingAsset = await _repository.GetByIdAsync(asset.Id);
        Assert.That(existingAsset, Is.Not.Null);

        // Act
        await _repository.DeleteAsync(asset);

        // Assert
        var deletedAsset = await _repository.GetByIdAsync(asset.Id);
        Assert.That(deletedAsset, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_Should_Publish_AssetDeletedEvent()
    {
        // Arrange
        var asset = Asset.New(
            new AssetName("Delete Event Test Asset"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "DEL2", AssetPriceSource.Manual, 100m, "USD"),
            Icon.Empty);

        await _repository.SaveAsync(asset);
        _domainEventPublisher.ClearReceivedCalls();

        // Act
        await _repository.DeleteAsync(asset);

        // Assert
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AssetDeletedEvent>());
    }

    [Test]
    public async Task DeleteAsync_Should_Clear_Events()
    {
        // Arrange
        var asset = AssetBuilder.AnAsset().WithName("Delete Events Clear Test").Build();
        await _repository.SaveAsync(asset);

        // Retrieve and modify to generate events
        var retrieved = await _repository.GetByIdAsync(asset.Id);
        retrieved!.UpdatePrice(999m);
        Assert.That(retrieved.Events.Count, Is.GreaterThan(0));

        // Act
        await _repository.DeleteAsync(retrieved);

        // Assert
        Assert.That(retrieved.Events, Is.Empty);
    }

    [Test]
    public async Task DeleteAsync_Should_Not_Affect_Other_Assets()
    {
        // Arrange
        var asset1 = Asset.New(
            new AssetName("Asset To Delete"),
            new BasicAssetDetails(AssetTypes.Stock, 10, "A1", AssetPriceSource.Manual, 100m, "USD"),
            Icon.Empty);

        var asset2 = Asset.New(
            new AssetName("Asset To Keep"),
            new BasicAssetDetails(AssetTypes.Stock, 20, "A2", AssetPriceSource.Manual, 200m, "USD"),
            Icon.Empty);

        await _repository.SaveAsync(asset1);
        await _repository.SaveAsync(asset2);

        // Act
        await _repository.DeleteAsync(asset1);

        // Assert
        var deletedAsset = await _repository.GetByIdAsync(asset1.Id);
        var remainingAsset = await _repository.GetByIdAsync(asset2.Id);

        Assert.That(deletedAsset, Is.Null);
        Assert.That(remainingAsset, Is.Not.Null);
        Assert.That(remainingAsset!.Name.Value, Is.EqualTo("Asset To Keep"));
    }

    #endregion
}
