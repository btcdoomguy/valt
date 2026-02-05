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
            entity.Version);
    }
}
