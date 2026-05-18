using Valt.App.Modules.Assets.Commands.MoveAssetToGroup;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class MoveAssetToGroupHandlerTests : DatabaseTest
{
    private MoveAssetToGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        var existingGroups = await _assetGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _assetGroupRepository.DeleteAsync(group.Id);

        _handler = new MoveAssetToGroupHandler(_assetRepository, _assetGroupRepository);
    }

    [Test]
    public async Task HandleAsync_MovesAsset_To_Group()
    {
        var group = AssetGroup.New(AssetGroupName.New("Target Group"));
        await _assetGroupRepository.SaveAsync(group);

        var asset = AssetBuilder.AnAsset()
            .WithName("Test Asset")
            .WithBasicDetails()
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = new MoveAssetToGroupCommand
        {
            AssetId = asset.Id.Value,
            TargetGroupId = group.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.That(updatedAsset!.GroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public async Task HandleAsync_RemovesAsset_From_Group()
    {
        var group = AssetGroup.New(AssetGroupName.New("Source Group"));
        await _assetGroupRepository.SaveAsync(group);

        var asset = AssetBuilder.AnAsset()
            .WithName("Test Asset")
            .WithGroupId(group.Id)
            .WithBasicDetails()
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = new MoveAssetToGroupCommand
        {
            AssetId = asset.Id.Value,
            TargetGroupId = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.That(updatedAsset!.GroupId, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyAssetId_ReturnsValidationError()
    {
        var command = new MoveAssetToGroupCommand
        {
            AssetId = "",
            TargetGroupId = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAssetId_ReturnsNotFound()
    {
        var command = new MoveAssetToGroupCommand
        {
            AssetId = "000000000000000000000001",
            TargetGroupId = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentTargetGroupId_ReturnsNotFound()
    {
        var asset = AssetBuilder.AnAsset()
            .WithName("Test Asset")
            .WithBasicDetails()
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = new MoveAssetToGroupCommand
        {
            AssetId = asset.Id.Value,
            TargetGroupId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GROUP_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_MovesAsset_Between_Groups()
    {
        var group1 = AssetGroup.New(AssetGroupName.New("Group 1"));
        var group2 = AssetGroup.New(AssetGroupName.New("Group 2"));
        await _assetGroupRepository.SaveAsync(group1);
        await _assetGroupRepository.SaveAsync(group2);

        var asset = AssetBuilder.AnAsset()
            .WithName("Test Asset")
            .WithGroupId(group1.Id)
            .WithBasicDetails()
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = new MoveAssetToGroupCommand
        {
            AssetId = asset.Id.Value,
            TargetGroupId = group2.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.That(updatedAsset!.GroupId, Is.EqualTo(group2.Id));
    }
}
