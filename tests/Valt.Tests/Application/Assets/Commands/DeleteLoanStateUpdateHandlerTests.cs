using Valt.App.Modules.Assets.Commands.DeleteLoanStateUpdate;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class DeleteLoanStateUpdateHandlerTests : DatabaseTest
{
    private DeleteLoanStateUpdateHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new DeleteLoanStateUpdateHandler(
            _assetRepository,
            new DeleteLoanStateUpdateValidator());
    }

    private static DeleteLoanStateUpdateCommand ValidCommand(string assetId, DateOnly effectiveDate) => new()
    {
        AssetId = assetId,
        EffectiveDate = effectiveDate
    };

    [Test]
    public async Task HandleAsync_WithInitialSnapshot_ReturnsCannotDeleteInitial()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(new DateOnly(2025, 6, 1), 26_000m)
            .WithSnapshot(new DateOnly(2025, 7, 1), 27_000m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 6, 1));
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CANNOT_DELETE_INITIAL_SNAPSHOT"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonInitialSnapshot_DeletesSnapshot()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(new DateOnly(2025, 6, 1), 26_000m)
            .WithSnapshot(new DateOnly(2025, 7, 1), 27_000m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 7, 1));
        var result = await _handler.HandleAsync(command);

        var saved = await _assetRepository.GetByIdAsync(asset.Id);
        var details = (BtcLoanDetails)saved!.Details;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(details.Snapshots, Has.Count.EqualTo(1));
            Assert.That(details.Snapshots.Any(s => s.EffectiveDate == new DateOnly(2025, 7, 1)), Is.False);
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentEffectiveDate_AllowsDelete()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(new DateOnly(2025, 6, 1), 26_000m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 9, 1));
        var result = await _handler.HandleAsync(command);

        var saved = await _assetRepository.GetByIdAsync(asset.Id);
        var details = (BtcLoanDetails)saved!.Details;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(details.Snapshots, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_WithMissingAsset_ReturnsNotFound()
    {
        var command = ValidCommand("000000000000000000000000", new DateOnly(2025, 6, 1));
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithStockAsset_ReturnsInvalidAssetType()
    {
        var asset = AssetBuilder.AStockAsset().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 6, 1));
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_ASSET_TYPE"));
        });
    }

    [Test]
    public async Task HandleAsync_WithDefaultEffectiveDate_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, default);
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyAssetId_ReturnsValidationFailed()
    {
        var command = ValidCommand("", new DateOnly(2025, 6, 1));
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
