using Valt.App.Modules.Budget.Accounts.Commands.EditAccountGroup;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class EditAccountGroupHandlerTests : DatabaseTest
{
    private EditAccountGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing account groups from previous tests
        var existingGroups = await _accountGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _accountGroupRepository.DeleteAsync(group.Id);

        _handler = new EditAccountGroupHandler(_accountGroupRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidData_UpdatesGroupName()
    {
        var group = AccountGroup.New(AccountGroupName.New("Original Name"));
        await _accountGroupRepository.SaveAsync(group);

        var command = new EditAccountGroupCommand
        {
            GroupId = group.Id.Value,
            Name = "Updated Name"
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedGroup = await _accountGroupRepository.GetByIdAsync(group.Id);
        Assert.That(updatedGroup!.Name.Value, Is.EqualTo("Updated Name"));
    }

    [Test]
    public async Task HandleAsync_WithEmptyGroupId_ReturnsValidationError()
    {
        var command = new EditAccountGroupCommand
        {
            GroupId = "",
            Name = "New Name"
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
        var group = AccountGroup.New(AccountGroupName.New("Test"));
        await _accountGroupRepository.SaveAsync(group);

        var command = new EditAccountGroupCommand
        {
            GroupId = group.Id.Value,
            Name = ""
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
        var command = new EditAccountGroupCommand
        {
            GroupId = "000000000000000000000001",
            Name = "New Name"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GROUP_NOT_FOUND"));
        });
    }
}
