using Valt.App.Modules.Assets.Commands.CreateAssetGroup;
using Valt.Core.Modules.Assets;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class CreateAssetGroupHandlerTests : DatabaseTest
{
    private CreateAssetGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        var existingGroups = await _assetGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _assetGroupRepository.DeleteAsync(group.Id);

        _handler = new CreateAssetGroupHandler(_assetGroupRepository, new CreateAssetGroupValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidNameAndDescription_CreatesGroup()
    {
        var command = new CreateAssetGroupCommand
        {
            Name = "Investments",
            Description = "All my investments"
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.Null.And.Not.Empty);

        var createdGroup = await _assetGroupRepository.GetByIdAsync(new AssetGroupId(result.Value!));
        Assert.That(createdGroup, Is.Not.Null);
        Assert.That(createdGroup!.Name.Value, Is.EqualTo("Investments"));
        Assert.That(createdGroup.Description, Is.EqualTo("All my investments"));
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateAssetGroupCommand { Name = "", Description = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithWhitespaceName_ReturnsValidationError()
    {
        var command = new CreateAssetGroupCommand { Name = "   ", Description = "" };

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
        var command = new CreateAssetGroupCommand
        {
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
    public async Task HandleAsync_CreatesMultipleGroupsWithUniqueIds()
    {
        var command1 = new CreateAssetGroupCommand { Name = "Group 1", Description = "" };
        var command2 = new CreateAssetGroupCommand { Name = "Group 2", Description = "" };

        var result1 = await _handler.HandleAsync(command1);
        var result2 = await _handler.HandleAsync(command2);

        Assert.That(result1.IsSuccess, Is.True);
        Assert.That(result2.IsSuccess, Is.True);
        Assert.That(result1.Value, Is.Not.EqualTo(result2.Value));
    }
}
