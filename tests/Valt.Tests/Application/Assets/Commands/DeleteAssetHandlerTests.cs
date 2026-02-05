using Valt.App.Modules.Assets.Commands.DeleteAsset;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class DeleteAssetHandlerTests : DatabaseTest
{
    private DeleteAssetHandler _handler = null!;
    private Asset _existingAsset = null!;

    protected override async Task SeedDatabase()
    {
        _existingAsset = AssetBuilder.AStockAsset("AAPL", 150m, 10).Build();
        await _assetRepository.SaveAsync(_existingAsset);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new DeleteAssetHandler(
            _assetRepository,
            new DeleteAssetValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidAssetId_DeletesAsset()
    {
        var command = new DeleteAssetCommand
        {
            AssetId = _existingAsset.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var deletedAsset = await _assetRepository.GetByIdAsync(_existingAsset.Id);
        Assert.That(deletedAsset, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAssetId_ReturnsNotFound()
    {
        var command = new DeleteAssetCommand
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
    public async Task HandleAsync_WithEmptyAssetId_ReturnsValidationError()
    {
        var command = new DeleteAssetCommand
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
