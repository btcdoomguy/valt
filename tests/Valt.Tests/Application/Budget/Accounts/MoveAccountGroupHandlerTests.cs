using Valt.App.Modules.Budget.Accounts.Commands.MoveAccountGroup;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Application.Budget.Accounts;

[TestFixture]
public class MoveAccountGroupHandlerTests : DatabaseTest
{
    private MoveAccountGroupHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing account groups from previous tests
        var existingGroups = await _accountGroupRepository.GetAllAsync();
        foreach (var group in existingGroups)
            await _accountGroupRepository.DeleteAsync(group.Id);

        _handler = new MoveAccountGroupHandler(_accountGroupRepository);
    }

    [Test]
    public async Task HandleAsync_MovesGroupToNewPosition()
    {
        // Create 3 groups with initial order
        var group1 = AccountGroup.New(AccountGroupName.New("Group 1"));
        group1.ChangeDisplayOrder(0);
        var group2 = AccountGroup.New(AccountGroupName.New("Group 2"));
        group2.ChangeDisplayOrder(1);
        var group3 = AccountGroup.New(AccountGroupName.New("Group 3"));
        group3.ChangeDisplayOrder(2);

        await _accountGroupRepository.SaveAsync(group1);
        await _accountGroupRepository.SaveAsync(group2);
        await _accountGroupRepository.SaveAsync(group3);

        // Move group 1 to position 2
        var command = new MoveAccountGroupCommand
        {
            GroupId = group1.Id.Value,
            NewDisplayOrder = 2
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        // Verify the new order
        var allGroups = (await _accountGroupRepository.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        Assert.That(allGroups[0].Name.Value, Is.EqualTo("Group 2"));
        Assert.That(allGroups[1].Name.Value, Is.EqualTo("Group 3"));
        Assert.That(allGroups[2].Name.Value, Is.EqualTo("Group 1"));
    }

    [Test]
    public async Task HandleAsync_WithEmptyGroupId_ReturnsValidationError()
    {
        var command = new MoveAccountGroupCommand
        {
            GroupId = "",
            NewDisplayOrder = 0
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
        var command = new MoveAccountGroupCommand
        {
            GroupId = "000000000000000000000001",
            NewDisplayOrder = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("GROUP_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_MovesToBeginning_UpdatesAllDisplayOrders()
    {
        var group1 = AccountGroup.New(AccountGroupName.New("Group 1"));
        group1.ChangeDisplayOrder(0);
        var group2 = AccountGroup.New(AccountGroupName.New("Group 2"));
        group2.ChangeDisplayOrder(1);

        await _accountGroupRepository.SaveAsync(group1);
        await _accountGroupRepository.SaveAsync(group2);

        // Move group 2 to position 0
        var command = new MoveAccountGroupCommand
        {
            GroupId = group2.Id.Value,
            NewDisplayOrder = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var allGroups = (await _accountGroupRepository.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        Assert.That(allGroups[0].Name.Value, Is.EqualTo("Group 2"));
        Assert.That(allGroups[1].Name.Value, Is.EqualTo("Group 1"));
    }
}
