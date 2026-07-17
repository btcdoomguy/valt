using Valt.App.Modules.Assets.Commands.UndoAssetSale;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class UndoAssetSaleHandlerTests : DatabaseTest
{
    private UndoAssetSaleHandler _handler = null!;
    private Asset _existingAsset = null!;

    protected override async Task SeedDatabase()
    {
        _existingAsset = AssetBuilder.AStockAsset("AAPL", 150m, 10)
            .WithVisible(false)
            .WithPreviousVisibility(true)
            .WithSold(true)
            .WithDateSold(new DateOnly(2025, 1, 15))
            .Build();
        await _assetRepository.SaveAsync(_existingAsset);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new UndoAssetSaleHandler(
            _assetRepository,
            new UndoAssetSaleValidator());
    }

    [Test]
    public async Task HandleAsync_UndoesSale_And_Restores_Visibility()
    {
        var command = new UndoAssetSaleCommand
        {
            AssetId = _existingAsset.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(_existingAsset.Id);
        Assert.That(updatedAsset, Is.Not.Null);
        Assert.That(updatedAsset!.IsSold, Is.False);
        Assert.That(updatedAsset.DateSold, Is.Null);
        Assert.That(updatedAsset.Visible, Is.True);
        Assert.That(updatedAsset.PreviousVisibility, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAsset_ReturnsNotFound()
    {
        var command = new UndoAssetSaleCommand
        {
            AssetId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithActiveAsset_ReturnsNotSoldError()
    {
        // Arrange
        var activeAsset = AssetBuilder.AStockAsset("TSLA", 300m, 5)
            .WithSold(false)
            .WithVisible(false)
            .Build();
        await _assetRepository.SaveAsync(activeAsset);

        var command = new UndoAssetSaleCommand
        {
            AssetId = activeAsset.Id.Value
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_SOLD"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyAssetId_ReturnsValidationError()
    {
        var command = new UndoAssetSaleCommand
        {
            AssetId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
