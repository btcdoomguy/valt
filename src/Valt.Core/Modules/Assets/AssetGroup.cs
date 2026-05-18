using Valt.Core.Kernel;

namespace Valt.Core.Modules.Assets;

public class AssetGroup : Entity<AssetGroupId>
{
    public AssetGroupName Name { get; private set; }
    public string Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public int Version { get; private set; }

    private AssetGroup(AssetGroupId id, AssetGroupName name, string description, int displayOrder, int version)
    {
        Id = id;
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        Version = version;
    }

    public static AssetGroup New(AssetGroupName name, string description = "")
        => new(new AssetGroupId(), name, description, int.MaxValue, 0);

    public static AssetGroup Create(AssetGroupId id, AssetGroupName name, string description, int displayOrder, int version)
        => new(id, name, description, displayOrder, version);

    public void Rename(AssetGroupName name)
    {
        if (Name == name)
            return;

        Name = name;
    }

    public void ChangeDescription(string description)
    {
        if (Description == description)
            return;

        Description = description;
    }

    public void ChangeDisplayOrder(int displayOrder)
    {
        if (DisplayOrder == displayOrder)
            return;

        DisplayOrder = displayOrder;
    }
}
