using Valt.App.Modules.Assets.Commands.DeleteAssetGroup;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class DeleteAssetGroupHandlerTests : DatabaseTest
{
    private DeleteAssetGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        var existingGroups = await _assetGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _assetGroupRepository.DeleteAsync(group.Id);

        _handler = new DeleteAssetGroupHandler(_assetGroupRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidGroupId_DeletesGroup()
    {
        var group = AssetGroup.New(AssetGroupName.New("To Be Deleted"));
        await _assetGroupRepository.SaveAsync(group);

        var command = new DeleteAssetGroupCommand { GroupId = group.Id.Value };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var deletedGroup = await _assetGroupRepository.GetByIdAsync(group.Id);
        Assert.That(deletedGroup, Is.Null);
    }

    [Test]
    public async Task HandleAsync_DeletesGroup_And_Unlinks_Assets()
    {
        var group = AssetGroup.New(AssetGroupName.New("With Assets"));
        await _assetGroupRepository.SaveAsync(group);

        var asset = AssetBuilder.AnAsset()
            .WithName("Linked Asset")
            .WithGroupId(group.Id)
            .WithBasicDetails()
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = new DeleteAssetGroupCommand { GroupId = group.Id.Value };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedAsset = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.That(updatedAsset!.GroupId, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyGroupId_ReturnsValidationError()
    {
        var command = new DeleteAssetGroupCommand { GroupId = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentGroupId_ReturnsNotFound()
    {
        var command = new DeleteAssetGroupCommand { GroupId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSETGROUP_NOT_FOUND"));
        });
    }
}
