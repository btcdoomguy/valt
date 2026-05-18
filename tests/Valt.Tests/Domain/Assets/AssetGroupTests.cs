using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Assets;
using Valt.Infra.Kernel;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Assets;

[TestFixture]
public class AssetGroupTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    #region AssetGroupName Validation Tests

    [Test]
    public void AssetGroupName_Should_Throw_Error_If_Empty()
    {
        Assert.Throws<ArgumentException>(() => AssetGroupName.New(""));
    }

    [Test]
    public void AssetGroupName_Should_Throw_Error_If_Name_Bigger_Than_50_Chars()
    {
        var longName = new string('a', 51);
        Assert.Throws<ArgumentException>(() => AssetGroupName.New(longName));
    }

    [Test]
    public void AssetGroupName_Should_Create_Valid_Name()
    {
        var name = AssetGroupName.New("My Group");
        Assert.That(name.Value, Is.EqualTo("My Group"));
    }

    [Test]
    public void AssetGroupName_Should_Allow_50_Chars()
    {
        var maxLengthName = new string('a', 50);
        var name = AssetGroupName.New(maxLengthName);
        Assert.That(name.Value, Is.EqualTo(maxLengthName));
    }

    [Test]
    public void AssetGroupName_Should_Throw_Error_If_Whitespace()
    {
        Assert.Throws<ArgumentException>(() => AssetGroupName.New("   "));
    }

    #endregion

    #region AssetGroup Entity Tests

    [Test]
    public void Should_Create_New_AssetGroup()
    {
        var group = AssetGroup.New(AssetGroupName.New("Investments"), "My investments");

        Assert.That(group.Name.Value, Is.EqualTo("Investments"));
        Assert.That(group.Description, Is.EqualTo("My investments"));
        Assert.That(group.DisplayOrder, Is.EqualTo(int.MaxValue));
        Assert.That(group.Version, Is.EqualTo(0));
        Assert.That(group.Id, Is.Not.Null);
    }

    [Test]
    public void Should_Create_AssetGroup_Without_Description()
    {
        var group = AssetGroup.New(AssetGroupName.New("Investments"));

        Assert.That(group.Description, Is.EqualTo(""));
    }

    [Test]
    public void Should_Rename_AssetGroup()
    {
        var group = AssetGroup.New(AssetGroupName.New("Old Name"));
        group.Rename(AssetGroupName.New("New Name"));

        Assert.That(group.Name.Value, Is.EqualTo("New Name"));
    }

    [Test]
    public void Should_Change_Description()
    {
        var group = AssetGroup.New(AssetGroupName.New("Test"), "Original");
        group.ChangeDescription("Updated");

        Assert.That(group.Description, Is.EqualTo("Updated"));
    }

    [Test]
    public void Should_Change_DisplayOrder()
    {
        var group = AssetGroup.New(AssetGroupName.New("Test"));
        group.ChangeDisplayOrder(3);

        Assert.That(group.DisplayOrder, Is.EqualTo(3));
    }

    [Test]
    public void Should_Create_AssetGroup_From_Existing_Data()
    {
        var id = new AssetGroupId();
        var name = AssetGroupName.New("Existing Group");
        var displayOrder = 2;
        var version = 1;

        var group = AssetGroup.Create(id, name, "Description", displayOrder, version);

        Assert.That(group.Id, Is.EqualTo(id));
        Assert.That(group.Name.Value, Is.EqualTo("Existing Group"));
        Assert.That(group.Description, Is.EqualTo("Description"));
        Assert.That(group.DisplayOrder, Is.EqualTo(2));
        Assert.That(group.Version, Is.EqualTo(1));
    }

    #endregion

    #region AssetGroup Builder Tests

    [Test]
    public void Builder_Should_Create_AssetGroup()
    {
        var group = AssetGroupBuilder.AnAssetGroup()
            .WithName("Builder Group")
            .WithDescription("Builder Desc")
            .WithDisplayOrder(2)
            .Build();

        Assert.That(group.Name.Value, Is.EqualTo("Builder Group"));
        Assert.That(group.Description, Is.EqualTo("Builder Desc"));
        Assert.That(group.DisplayOrder, Is.EqualTo(2));
    }

    [Test]
    public void Builder_Should_Create_StockGroup()
    {
        var group = AssetGroupBuilder.AStockGroup().Build();

        Assert.That(group.Name.Value, Is.EqualTo("Stocks"));
        Assert.That(group.Description, Is.EqualTo("Stock investments"));
    }

    [Test]
    public void Builder_Should_Create_RealEstateGroup()
    {
        var group = AssetGroupBuilder.ARealEstateGroup().Build();

        Assert.That(group.Name.Value, Is.EqualTo("Real Estate"));
        Assert.That(group.Description, Is.EqualTo("Property investments"));
    }

    #endregion

    #region Asset AssignToGroup Tests

    [Test]
    public void Asset_Should_Assign_To_Group()
    {
        var groupId = new AssetGroupId();
        var asset = AssetBuilder.AnAsset()
            .WithName("Test Asset")
            .WithBasicDetails()
            .Build();

        asset.AssignToGroup(groupId);

        Assert.That(asset.GroupId, Is.EqualTo(groupId));
    }

    [Test]
    public void Asset_Should_Unassign_From_Group()
    {
        var groupId = new AssetGroupId();
        var asset = AssetBuilder.AnAsset()
            .WithName("Test Asset")
            .WithGroupId(groupId)
            .WithBasicDetails()
            .Build();

        asset.AssignToGroup(null);

        Assert.That(asset.GroupId, Is.Null);
    }

    [Test]
    public void Asset_Should_Change_Group()
    {
        var groupId1 = new AssetGroupId();
        var groupId2 = new AssetGroupId();
        var asset = AssetBuilder.AnAsset()
            .WithName("Test Asset")
            .WithGroupId(groupId1)
            .WithBasicDetails()
            .Build();

        asset.AssignToGroup(groupId2);

        Assert.That(asset.GroupId, Is.EqualTo(groupId2));
    }

    #endregion
}
