using Valt.App.Modules.Assets.Commands.MarkAssetAsSold;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class MarkAssetAsSoldHandlerTests : DatabaseTest
{
    private MarkAssetAsSoldHandler _handler = null!;
    private Asset _existingAsset = null!;
    private FakeClock _clock = null!;

    protected override async Task SeedDatabase()
    {
        _existingAsset = AssetBuilder.AStockAsset("AAPL", 150m, 10)
            .WithVisible(true)
            .Build();
        await _assetRepository.SaveAsync(_existingAsset);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _clock = new FakeClock(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        _handler = new MarkAssetAsSoldHandler(
            _assetRepository,
            new MarkAssetAsSoldValidator(_clock),
            _clock);
    }

    [Test]
    public async Task HandleAsync_MarksAssetAsSold()
    {
        var freshAsset = AssetBuilder.AStockAsset("AAPL", 150m, 10)
            .WithVisible(true)
            .Build();
        await _assetRepository.SaveAsync(freshAsset);

        var command = new MarkAssetAsSoldCommand
        {
            AssetId = freshAsset.Id.Value,
            DateSold = new DateOnly(2025, 1, 15)
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(freshAsset.Id);
        Assert.That(updatedAsset, Is.Not.Null);
        Assert.That(updatedAsset!.IsSold, Is.True);
        Assert.That(updatedAsset.DateSold, Is.EqualTo(new DateOnly(2025, 1, 15)));
        Assert.That(updatedAsset.Visible, Is.False);
        Assert.That(updatedAsset.PreviousVisibility, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithPastDate_Succeeds()
    {
        var freshAsset = AssetBuilder.AStockAsset("MSFT", 200m, 5)
            .WithVisible(true)
            .Build();
        await _assetRepository.SaveAsync(freshAsset);

        var command = new MarkAssetAsSoldCommand
        {
            AssetId = freshAsset.Id.Value,
            DateSold = new DateOnly(2024, 12, 31)
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(freshAsset.Id);
        Assert.That(updatedAsset!.DateSold, Is.EqualTo(new DateOnly(2024, 12, 31)));
    }

    [Test]
    public async Task HandleAsync_WithFutureDate_ReturnsValidationError()
    {
        var command = new MarkAssetAsSoldCommand
        {
            AssetId = _existingAsset.Id.Value,
            DateSold = new DateOnly(2025, 12, 31)
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNullDate_DefaultsToToday()
    {
        var freshAsset = AssetBuilder.AStockAsset("GOOGL", 250m, 8)
            .WithVisible(true)
            .Build();
        await _assetRepository.SaveAsync(freshAsset);

        var command = new MarkAssetAsSoldCommand
        {
            AssetId = freshAsset.Id.Value,
            DateSold = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(freshAsset.Id);
        Assert.That(updatedAsset!.DateSold, Is.EqualTo(new DateOnly(2025, 6, 15)));
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAsset_ReturnsNotFound()
    {
        var command = new MarkAssetAsSoldCommand
        {
            AssetId = "000000000000000000000001",
            DateSold = new DateOnly(2025, 1, 15)
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithAlreadySoldAsset_ReturnsAlreadySoldError()
    {
        // Arrange
        var soldAsset = AssetBuilder.AStockAsset("TSLA", 300m, 5)
            .WithSold(true)
            .WithDateSold(new DateOnly(2025, 1, 10))
            .Build();
        await _assetRepository.SaveAsync(soldAsset);

        var command = new MarkAssetAsSoldCommand
        {
            AssetId = soldAsset.Id.Value,
            DateSold = new DateOnly(2025, 1, 15)
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_ALREADY_SOLD"));
        });
    }
}
