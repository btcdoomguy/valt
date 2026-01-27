using Valt.App.Modules.Budget.Accounts.Commands.DeleteAccountGroup;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class DeleteAccountGroupHandlerTests : DatabaseTest
{
    private DeleteAccountGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing account groups from previous tests
        var existingGroups = await _accountGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _accountGroupRepository.DeleteAsync(group.Id);

        _handler = new DeleteAccountGroupHandler(_accountGroupRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidGroupId_DeletesGroup()
    {
        var group = AccountGroup.New(AccountGroupName.New("To Be Deleted"));
        await _accountGroupRepository.SaveAsync(group);

        var command = new DeleteAccountGroupCommand { GroupId = group.Id.Value };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var deletedGroup = await _accountGroupRepository.GetByIdAsync(group.Id);
        Assert.That(deletedGroup, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyGroupId_ReturnsValidationError()
    {
        var command = new DeleteAccountGroupCommand { GroupId = "" };

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
        var command = new DeleteAccountGroupCommand { GroupId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GROUP_NOT_FOUND"));
        });
    }
}
