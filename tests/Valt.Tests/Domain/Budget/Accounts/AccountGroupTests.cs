using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Exceptions;
using Valt.Infra;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Accounts;

/// <summary>
/// Tests for AccountGroup entity and AccountGroupName value object.
/// </summary>
[TestFixture]
public class AccountGroupTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    #region AccountGroupName Validation Tests

    [Test]
    public void AccountGroupName_Should_Throw_Error_If_Empty()
    {
        // Act & Assert: Empty name should throw
        Assert.Throws<EmptyAccountGroupNameException>(() => AccountGroupName.New(""));
    }

    [Test]
    public void AccountGroupName_Should_Throw_Error_If_Name_Bigger_Than_50_Chars()
    {
        // Arrange: Create a name with 51 characters
        var longName = new string('a', 51);

        // Act & Assert: Should throw AccountGroupNameLengthException
        Assert.Throws<AccountGroupNameLengthException>(() => AccountGroupName.New(longName));
    }

    [Test]
    public void AccountGroupName_Should_Create_Valid_Name()
    {
        // Arrange & Act
        var name = AccountGroupName.New("My Group");

        // Assert
        Assert.That(name.Value, Is.EqualTo("My Group"));
    }

    [Test]
    public void AccountGroupName_Should_Allow_50_Chars()
    {
        // Arrange
        var maxLengthName = new string('a', 50);

        // Act
        var name = AccountGroupName.New(maxLengthName);

        // Assert
        Assert.That(name.Value, Is.EqualTo(maxLengthName));
    }

    #endregion

    #region AccountGroup Entity Tests

    [Test]
    public void Should_Create_New_AccountGroup()
    {
        // Act
        var group = AccountGroup.New(AccountGroupName.New("Savings"));

        // Assert
        Assert.That(group.Name.Value, Is.EqualTo("Savings"));
        Assert.That(group.DisplayOrder, Is.EqualTo(int.MaxValue)); // New groups get max value for ordering
        Assert.That(group.Version, Is.EqualTo(0)); // New groups start at version 0
        Assert.That(group.Id, Is.Not.Null);
    }

    [Test]
    public void Should_Rename_AccountGroup()
    {
        // Arrange
        var group = AccountGroup.New(AccountGroupName.New("Old Name"));

        // Act
        group.Rename(AccountGroupName.New("New Name"));

        // Assert
        Assert.That(group.Name.Value, Is.EqualTo("New Name"));
    }

    [Test]
    public void Should_Change_DisplayOrder()
    {
        // Arrange
        var group = AccountGroup.New(AccountGroupName.New("Test Group"));

        // Act
        group.ChangeDisplayOrder(5);

        // Assert
        Assert.That(group.DisplayOrder, Is.EqualTo(5));
    }

    [Test]
    public void Should_Create_AccountGroup_From_Existing_Data()
    {
        // Arrange
        var id = new AccountGroupId();
        var name = AccountGroupName.New("Existing Group");
        var displayOrder = 3;
        var version = 2;

        // Act
        var group = AccountGroup.Create(id, name, displayOrder, version);

        // Assert
        Assert.That(group.Id, Is.EqualTo(id));
        Assert.That(group.Name.Value, Is.EqualTo("Existing Group"));
        Assert.That(group.DisplayOrder, Is.EqualTo(3));
        Assert.That(group.Version, Is.EqualTo(2));
    }

    #endregion

    #region AccountGroup Builder Tests

    [Test]
    public void Builder_Should_Create_AccountGroup()
    {
        // Arrange & Act
        var group = AccountGroupBuilder.AGroup()
            .WithName("Builder Group")
            .WithDisplayOrder(2)
            .Build();

        // Assert
        Assert.That(group.Name.Value, Is.EqualTo("Builder Group"));
        Assert.That(group.DisplayOrder, Is.EqualTo(2));
    }

    [Test]
    public void Builder_Should_Create_AccountGroupEntity()
    {
        // Arrange & Act
        var entity = AccountGroupBuilder.AGroup()
            .WithName("Entity Group")
            .WithDisplayOrder(1)
            .WithVersion(3)
            .BuildEntity();

        // Assert
        Assert.That(entity.Name, Is.EqualTo("Entity Group"));
        Assert.That(entity.DisplayOrder, Is.EqualTo(1));
        Assert.That(entity.Version, Is.EqualTo(3));
    }

    #endregion

    #region Account AssignToGroup Tests

    [Test]
    public void FiatAccount_Should_Assign_To_Group()
    {
        // Arrange
        var groupId = new AccountGroupId();
        var account = FiatAccountBuilder.AnAccount()
            .WithName("Checking")
            .Build()
            .AsDomainObject();

        // Act
        account.AssignToGroup(groupId);

        // Assert
        Assert.That(account.GroupId, Is.EqualTo(groupId));
    }

    [Test]
    public void FiatAccount_Should_Unassign_From_Group()
    {
        // Arrange
        var groupId = new AccountGroupId();
        var account = FiatAccountBuilder.AnAccount()
            .WithName("Checking")
            .WithGroupId(groupId)
            .Build()
            .AsDomainObject();

        // Act
        account.AssignToGroup(null);

        // Assert
        Assert.That(account.GroupId, Is.Null);
    }

    [Test]
    public void BtcAccount_Should_Assign_To_Group()
    {
        // Arrange
        var groupId = new AccountGroupId();
        var account = BtcAccountBuilder.AnAccount()
            .WithName("Bitcoin Wallet")
            .Build()
            .AsDomainObject();

        // Act
        account.AssignToGroup(groupId);

        // Assert
        Assert.That(account.GroupId, Is.EqualTo(groupId));
    }

    #endregion
}
