using Valt.App.Modules.Budget.Accounts.Commands.CreateAccountGroup;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class CreateAccountGroupHandlerTests : DatabaseTest
{
    private CreateAccountGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing account groups from previous tests
        var existingGroups = await _accountGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _accountGroupRepository.DeleteAsync(group.Id);

        _handler = new CreateAccountGroupHandler(_accountGroupRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidName_CreatesGroup()
    {
        var command = new CreateAccountGroupCommand { Name = "Savings" };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.GroupId, Is.Not.Null.And.Not.Empty);

        var createdGroup = await _accountGroupRepository.GetByIdAsync(new AccountGroupId(result.Value.GroupId));
        Assert.That(createdGroup, Is.Not.Null);
        Assert.That(createdGroup!.Name.Value, Is.EqualTo("Savings"));
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateAccountGroupCommand { Name = "" };

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
        var command = new CreateAccountGroupCommand { Name = "   " };

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
        var command1 = new CreateAccountGroupCommand { Name = "Group 1" };
        var command2 = new CreateAccountGroupCommand { Name = "Group 2" };

        var result1 = await _handler.HandleAsync(command1);
        var result2 = await _handler.HandleAsync(command2);

        Assert.That(result1.IsSuccess, Is.True);
        Assert.That(result2.IsSuccess, Is.True);
        Assert.That(result1.Value!.GroupId, Is.Not.EqualTo(result2.Value!.GroupId));
    }
}
