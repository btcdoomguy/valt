using Valt.App.Modules.Assets.Commands.SetAssetVisibility;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class SetAssetVisibilityHandlerTests : DatabaseTest
{
    private SetAssetVisibilityHandler _handler = null!;
    private Asset _existingAsset = null!;

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
        _handler = new SetAssetVisibilityHandler(
            _assetRepository,
            new SetAssetVisibilityValidator());
    }

    [Test]
    public async Task HandleAsync_SetsAssetToInvisible()
    {
        var command = new SetAssetVisibilityCommand
        {
            AssetId = _existingAsset.Id.Value,
            Visible = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(_existingAsset.Id);
        Assert.That(updatedAsset!.Visible, Is.False);
    }

    [Test]
    public async Task HandleAsync_SetsAssetToVisible()
    {
        // First make it invisible
        _existingAsset.SetVisibility(false);
        await _assetRepository.SaveAsync(_existingAsset);

        var command = new SetAssetVisibilityCommand
        {
            AssetId = _existingAsset.Id.Value,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(_existingAsset.Id);
        Assert.That(updatedAsset!.Visible, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAsset_ReturnsNotFound()
    {
        var command = new SetAssetVisibilityCommand
        {
            AssetId = "000000000000000000000001",
            Visible = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyAssetId_ReturnsValidationError()
    {
        var command = new SetAssetVisibilityCommand
        {
            AssetId = "",
            Visible = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
