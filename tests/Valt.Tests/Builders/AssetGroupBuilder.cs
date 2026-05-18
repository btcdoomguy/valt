using Valt.Core.Modules.Assets;

namespace Valt.Tests.Builders;

public class AssetGroupBuilder
{
    private AssetGroupId _id = new();
    private AssetGroupName _name = AssetGroupName.New("Test Group");
    private string _description = string.Empty;
    private int _displayOrder = 0;
    private int _version = 1;

    public AssetGroupBuilder WithId(AssetGroupId id)
    {
        _id = id;
        return this;
    }

    public AssetGroupBuilder WithName(string name)
    {
        _name = AssetGroupName.New(name);
        return this;
    }

    public AssetGroupBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public AssetGroupBuilder WithDisplayOrder(int displayOrder)
    {
        _displayOrder = displayOrder;
        return this;
    }

    public AssetGroupBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public AssetGroup Build()
    {
        return AssetGroup.Create(_id, _name, _description, _displayOrder, _version);
    }

    public static AssetGroupBuilder AnAssetGroup() => new();

    public static AssetGroupBuilder AStockGroup() =>
        new AssetGroupBuilder().WithName("Stocks").WithDescription("Stock investments");

    public static AssetGroupBuilder ARealEstateGroup() =>
        new AssetGroupBuilder().WithName("Real Estate").WithDescription("Property investments");
}
