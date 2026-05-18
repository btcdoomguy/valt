using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;

namespace Valt.Infra.Modules.Assets;

internal static class Extensions
{
    public static AssetEntity AsEntity(this Asset asset)
    {
        return new AssetEntity
        {
            Id = new ObjectId(asset.Id.ToString()),
            Name = asset.Name.Value,
            AssetTypeId = (int)asset.Details.AssetType,
            DetailsJson = AssetDetailsSerializer.Serialize(asset.Details),
            Icon = asset.Icon.ToString(),
            IncludeInNetWorth = asset.IncludeInNetWorth,
            Visible = asset.Visible,
            LastPriceUpdateAt = asset.LastPriceUpdateAt,
            CreatedAt = asset.CreatedAt,
            DisplayOrder = asset.DisplayOrder,
            GroupId = asset.GroupId is not null ? new ObjectId(asset.GroupId.Value) : null,
            Version = asset.Version
        };
    }

    public static Asset AsDomainObject(this AssetEntity entity)
    {
        var assetType = (AssetTypes)entity.AssetTypeId;
        var details = AssetDetailsSerializer.DeserializeDetails(assetType, entity.DetailsJson);

        return Asset.Create(
            new AssetId(entity.Id.ToString()),
            new AssetName(entity.Name),
            details,
            Icon.RestoreFromId(entity.Icon ?? string.Empty),
            entity.IncludeInNetWorth,
            entity.Visible,
            entity.LastPriceUpdateAt,
            entity.CreatedAt,
            entity.DisplayOrder,
            entity.GroupId is not null ? new AssetGroupId(entity.GroupId.ToString()) : null,
            entity.Version);
    }

    public static AssetGroupEntity AsEntity(this AssetGroup group)
    {
        return new AssetGroupEntity
        {
            Id = new ObjectId(group.Id.Value),
            Name = group.Name.Value,
            Description = group.Description,
            DisplayOrder = group.DisplayOrder,
            Version = group.Version
        };
    }

    public static AssetGroup AsDomainObject(this AssetGroupEntity entity)
    {
        return AssetGroup.Create(
            new AssetGroupId(entity.Id.ToString()),
            AssetGroupName.New(entity.Name),
            entity.Description,
            entity.DisplayOrder,
            entity.Version);
    }
}
