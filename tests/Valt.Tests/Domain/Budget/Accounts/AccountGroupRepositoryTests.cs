using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Accounts;

/// <summary>
/// Tests for AccountGroupRepository including display order functionality.
/// </summary>
[TestFixture]
public class AccountGroupRepositoryTests
{
    private MemoryStream _localDatabaseStream;
    private ILocalDatabase _localDatabase;
    private AccountGroupRepository _repository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());

        _localDatabaseStream = new MemoryStream();
        _localDatabase = new LocalDatabase(new Clock());
        _localDatabase.OpenInMemoryDatabase(_localDatabaseStream);
    }

    [SetUp]
    public void SetUp()
    {
        _repository = new AccountGroupRepository(_localDatabase);

        // Clear existing groups before each test
        var existingGroups = _localDatabase.GetAccountGroups().FindAll().ToList();
        foreach (var group in existingGroups)
        {
            _localDatabase.GetAccountGroups().Delete(group.Id);
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _localDatabase.CloseDatabase();
        _localDatabase.Dispose();
        await _localDatabaseStream.DisposeAsync();
    }

    #region Basic CRUD Tests

    [Test]
    public async Task SaveAsync_Should_Store_And_Retrieve_New_Group()
    {
        // Arrange
        var group = AccountGroupBuilder.AGroup()
            .WithName("Test Group")
            .WithDisplayOrder(0)
            .Build();

        // Act
        await _repository.SaveAsync(group);

        // Assert
        var retrievedGroup = await _repository.GetByIdAsync(group.Id);
        Assert.That(retrievedGroup, Is.Not.Null);
        Assert.That(retrievedGroup!.Id, Is.EqualTo(group.Id));
        Assert.That(retrievedGroup.Name.Value, Is.EqualTo("Test Group"));
        Assert.That(retrievedGroup.DisplayOrder, Is.EqualTo(0));
    }

    [Test]
    public async Task SaveAsync_Should_Update_Existing_Group()
    {
        // Arrange
        var group = AccountGroupBuilder.AGroup()
            .WithName("Original Name")
            .WithDisplayOrder(0)
            .Build();

        await _repository.SaveAsync(group);

        // Act
        group.Rename(AccountGroupName.New("Updated Name"));
        group.ChangeDisplayOrder(5);
        await _repository.SaveAsync(group);

        // Assert
        var retrievedGroup = await _repository.GetByIdAsync(group.Id);
        Assert.That(retrievedGroup, Is.Not.Null);
        Assert.That(retrievedGroup!.Name.Value, Is.EqualTo("Updated Name"));
        Assert.That(retrievedGroup.DisplayOrder, Is.EqualTo(5));
    }

    [Test]
    public async Task DeleteAsync_Should_Remove_Group_From_Database()
    {
        // Arrange
        var group = AccountGroupBuilder.AGroup()
            .WithName("To Delete")
            .Build();

        await _repository.SaveAsync(group);

        // Verify group exists
        var existingGroup = await _repository.GetByIdAsync(group.Id);
        Assert.That(existingGroup, Is.Not.Null);

        // Act
        await _repository.DeleteAsync(group.Id);

        // Assert
        var deletedGroup = await _repository.GetByIdAsync(group.Id);
        Assert.That(deletedGroup, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_Group()
    {
        // Arrange
        var nonExistentId = new AccountGroupId();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region GetAllAsync Ordering Tests

    [Test]
    public async Task GetAllAsync_Should_Return_Groups_Ordered_By_DisplayOrder()
    {
        // Arrange
        var group1 = AccountGroupBuilder.AGroup()
            .WithName("Third")
            .WithDisplayOrder(2)
            .Build();

        var group2 = AccountGroupBuilder.AGroup()
            .WithName("First")
            .WithDisplayOrder(0)
            .Build();

        var group3 = AccountGroupBuilder.AGroup()
            .WithName("Second")
            .WithDisplayOrder(1)
            .Build();

        await _repository.SaveAsync(group1);
        await _repository.SaveAsync(group2);
        await _repository.SaveAsync(group3);

        // Act
        var groups = (await _repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(groups, Has.Count.EqualTo(3));
        Assert.That(groups[0].Name.Value, Is.EqualTo("First"));
        Assert.That(groups[1].Name.Value, Is.EqualTo("Second"));
        Assert.That(groups[2].Name.Value, Is.EqualTo("Third"));
    }

    [Test]
    public async Task GetAllAsync_Should_Return_Empty_List_When_No_Groups_Exist()
    {
        // Act
        var groups = await _repository.GetAllAsync();

        // Assert
        Assert.That(groups, Is.Empty);
    }

    #endregion

    #region Display Order Change Tests

    [Test]
    public async Task ChangeDisplayOrder_Should_Persist_New_Order()
    {
        // Arrange
        var group = AccountGroupBuilder.AGroup()
            .WithName("Test Group")
            .WithDisplayOrder(0)
            .Build();

        await _repository.SaveAsync(group);

        // Act
        group.ChangeDisplayOrder(10);
        await _repository.SaveAsync(group);

        // Assert
        var retrievedGroup = await _repository.GetByIdAsync(group.Id);
        Assert.That(retrievedGroup!.DisplayOrder, Is.EqualTo(10));
    }

    [Test]
    public async Task Swapping_DisplayOrders_Should_Change_Group_Order()
    {
        // Arrange - Create two groups with specific orders
        var groupA = AccountGroupBuilder.AGroup()
            .WithName("Group A")
            .WithDisplayOrder(0)
            .Build();

        var groupB = AccountGroupBuilder.AGroup()
            .WithName("Group B")
            .WithDisplayOrder(1)
            .Build();

        await _repository.SaveAsync(groupA);
        await _repository.SaveAsync(groupB);

        // Verify initial order
        var initialGroups = (await _repository.GetAllAsync()).ToList();
        Assert.That(initialGroups[0].Name.Value, Is.EqualTo("Group A"));
        Assert.That(initialGroups[1].Name.Value, Is.EqualTo("Group B"));

        // Act - Swap display orders
        groupA.ChangeDisplayOrder(1);
        groupB.ChangeDisplayOrder(0);
        await _repository.SaveAsync(groupA);
        await _repository.SaveAsync(groupB);

        // Assert - Order should be reversed
        var swappedGroups = (await _repository.GetAllAsync()).ToList();
        Assert.That(swappedGroups[0].Name.Value, Is.EqualTo("Group B"));
        Assert.That(swappedGroups[1].Name.Value, Is.EqualTo("Group A"));
    }

    #endregion

    #region Move Up/Down Logic Tests

    [Test]
    public async Task MoveUp_FirstGroup_Should_Not_Change_Order()
    {
        // Arrange
        var group1 = AccountGroupBuilder.AGroup()
            .WithName("First")
            .WithDisplayOrder(0)
            .Build();

        var group2 = AccountGroupBuilder.AGroup()
            .WithName("Second")
            .WithDisplayOrder(1)
            .Build();

        await _repository.SaveAsync(group1);
        await _repository.SaveAsync(group2);

        // Act - Try to move first group up (should do nothing)
        var allGroups = (await _repository.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        var currentIndex = allGroups.FindIndex(g => g.Id == group1.Id);

        // First group can't move up
        Assert.That(currentIndex, Is.EqualTo(0));

        // Assert - Order unchanged
        var groups = (await _repository.GetAllAsync()).ToList();
        Assert.That(groups[0].Name.Value, Is.EqualTo("First"));
        Assert.That(groups[1].Name.Value, Is.EqualTo("Second"));
    }

    [Test]
    public async Task MoveDown_LastGroup_Should_Not_Change_Order()
    {
        // Arrange
        var group1 = AccountGroupBuilder.AGroup()
            .WithName("First")
            .WithDisplayOrder(0)
            .Build();

        var group2 = AccountGroupBuilder.AGroup()
            .WithName("Second")
            .WithDisplayOrder(1)
            .Build();

        await _repository.SaveAsync(group1);
        await _repository.SaveAsync(group2);

        // Act - Try to move last group down (should do nothing)
        var allGroups = (await _repository.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        var currentIndex = allGroups.FindIndex(g => g.Id == group2.Id);

        // Last group can't move down
        Assert.That(currentIndex, Is.EqualTo(allGroups.Count - 1));

        // Assert - Order unchanged
        var groups = (await _repository.GetAllAsync()).ToList();
        Assert.That(groups[0].Name.Value, Is.EqualTo("First"));
        Assert.That(groups[1].Name.Value, Is.EqualTo("Second"));
    }

    [Test]
    public async Task MoveUp_MiddleGroup_Should_Swap_With_Previous()
    {
        // Arrange - Create three groups
        var group1 = AccountGroupBuilder.AGroup()
            .WithName("First")
            .WithDisplayOrder(0)
            .Build();

        var group2 = AccountGroupBuilder.AGroup()
            .WithName("Second")
            .WithDisplayOrder(1)
            .Build();

        var group3 = AccountGroupBuilder.AGroup()
            .WithName("Third")
            .WithDisplayOrder(2)
            .Build();

        await _repository.SaveAsync(group1);
        await _repository.SaveAsync(group2);
        await _repository.SaveAsync(group3);

        // Act - Move "Second" up (swap with "First")
        var allGroups = (await _repository.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        var currentIndex = allGroups.FindIndex(g => g.Name.Value == "Second");
        var targetIndex = currentIndex - 1;

        // Swap positions in list
        (allGroups[currentIndex], allGroups[targetIndex]) = (allGroups[targetIndex], allGroups[currentIndex]);

        // Re-assign sequential display orders
        for (var i = 0; i < allGroups.Count; i++)
        {
            allGroups[i].ChangeDisplayOrder(i);
            await _repository.SaveAsync(allGroups[i]);
        }

        // Assert - Second is now first
        var groups = (await _repository.GetAllAsync()).ToList();
        Assert.That(groups[0].Name.Value, Is.EqualTo("Second"));
        Assert.That(groups[1].Name.Value, Is.EqualTo("First"));
        Assert.That(groups[2].Name.Value, Is.EqualTo("Third"));
    }

    [Test]
    public async Task MoveDown_MiddleGroup_Should_Swap_With_Next()
    {
        // Arrange - Create three groups
        var group1 = AccountGroupBuilder.AGroup()
            .WithName("First")
            .WithDisplayOrder(0)
            .Build();

        var group2 = AccountGroupBuilder.AGroup()
            .WithName("Second")
            .WithDisplayOrder(1)
            .Build();

        var group3 = AccountGroupBuilder.AGroup()
            .WithName("Third")
            .WithDisplayOrder(2)
            .Build();

        await _repository.SaveAsync(group1);
        await _repository.SaveAsync(group2);
        await _repository.SaveAsync(group3);

        // Act - Move "Second" down (swap with "Third")
        var allGroups = (await _repository.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        var currentIndex = allGroups.FindIndex(g => g.Name.Value == "Second");
        var targetIndex = currentIndex + 1;

        // Swap positions in list
        (allGroups[currentIndex], allGroups[targetIndex]) = (allGroups[targetIndex], allGroups[currentIndex]);

        // Re-assign sequential display orders
        for (var i = 0; i < allGroups.Count; i++)
        {
            allGroups[i].ChangeDisplayOrder(i);
            await _repository.SaveAsync(allGroups[i]);
        }

        // Assert - Second is now third
        var groups = (await _repository.GetAllAsync()).ToList();
        Assert.That(groups[0].Name.Value, Is.EqualTo("First"));
        Assert.That(groups[1].Name.Value, Is.EqualTo("Third"));
        Assert.That(groups[2].Name.Value, Is.EqualTo("Second"));
    }

    [Test]
    public async Task Normalizing_DisplayOrders_Should_Fix_Gaps_And_Duplicates()
    {
        // Arrange - Create groups with non-sequential display orders (gaps and large values)
        var group1 = AccountGroupBuilder.AGroup()
            .WithName("First")
            .WithDisplayOrder(int.MaxValue) // New groups start with MaxValue
            .Build();

        var group2 = AccountGroupBuilder.AGroup()
            .WithName("Second")
            .WithDisplayOrder(int.MaxValue) // Same MaxValue (duplicate)
            .Build();

        var group3 = AccountGroupBuilder.AGroup()
            .WithName("Third")
            .WithDisplayOrder(5) // Gap in sequence
            .Build();

        await _repository.SaveAsync(group1);
        await _repository.SaveAsync(group2);
        await _repository.SaveAsync(group3);

        // Act - Normalize by re-assigning sequential orders
        var allGroups = (await _repository.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        for (var i = 0; i < allGroups.Count; i++)
        {
            allGroups[i].ChangeDisplayOrder(i);
            await _repository.SaveAsync(allGroups[i]);
        }

        // Assert - Groups now have sequential orders 0, 1, 2
        var normalizedGroups = (await _repository.GetAllAsync()).ToList();
        Assert.That(normalizedGroups[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(normalizedGroups[1].DisplayOrder, Is.EqualTo(1));
        Assert.That(normalizedGroups[2].DisplayOrder, Is.EqualTo(2));
    }

    #endregion
}
