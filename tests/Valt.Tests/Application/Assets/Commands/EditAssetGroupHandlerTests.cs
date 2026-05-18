using Valt.App.Modules.Assets.Commands.EditAssetGroup;
using Valt.Core.Modules.Assets;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class EditAssetGroupHandlerTests : DatabaseTest
{
    private EditAssetGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        var existingGroups = await _assetGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _assetGroupRepository.DeleteAsync(group.Id);

        _handler = new EditAssetGroupHandler(_assetGroupRepository, new EditAssetGroupValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidData_UpdatesGroupNameAndDescription()
    {
        var group = AssetGroup.New(AssetGroupName.New("Original Name"), "Original Desc");
        await _assetGroupRepository.SaveAsync(group);

        var command = new EditAssetGroupCommand
        {
            GroupId = group.Id.Value,
            Name = "Updated Name",
            Description = "Updated Desc"
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedGroup = await _assetGroupRepository.GetByIdAsync(group.Id);
        Assert.That(updatedGroup!.Name.Value, Is.EqualTo("Updated Name"));
        Assert.That(updatedGroup.Description, Is.EqualTo("Updated Desc"));
    }

    [Test]
    public async Task HandleAsync_WithEmptyGroupId_ReturnsValidationError()
    {
        var command = new EditAssetGroupCommand
        {
            GroupId = "",
            Name = "New Name",
            Description = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var group = AssetGroup.New(AssetGroupName.New("Test"));
        await _assetGroupRepository.SaveAsync(group);

        var command = new EditAssetGroupCommand
        {
            GroupId = group.Id.Value,
            Name = "",
            Description = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNameLongerThan50Chars_ReturnsValidationError()
    {
        var group = AssetGroup.New(AssetGroupName.New("Test"));
        await _assetGroupRepository.SaveAsync(group);

        var command = new EditAssetGroupCommand
        {
            GroupId = group.Id.Value,
            Name = new string('a', 51),
            Description = ""
        };

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
        var command = new EditAssetGroupCommand
        {
            GroupId = "000000000000000000000001",
            Name = "New Name",
            Description = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSETGROUP_NOT_FOUND"));
        });
    }
}
