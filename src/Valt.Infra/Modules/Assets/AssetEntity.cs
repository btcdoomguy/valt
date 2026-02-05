using LiteDB;

namespace Valt.Infra.Modules.Assets;

public class AssetEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;

    [BsonField("name")] public string Name { get; set; } = null!;
    [BsonField("assetType")] public int AssetTypeId { get; set; }
    [BsonField("detailsJson")] public string DetailsJson { get; set; } = null!;
    [BsonField("icon")] public string? Icon { get; set; }
    [BsonField("includeInNetWorth")] public bool IncludeInNetWorth { get; set; }
    [BsonField("visible")] public bool Visible { get; set; }
    [BsonField("lastPriceUpdate")] public DateTime LastPriceUpdateAt { get; set; }
    [BsonField("created")] public DateTime CreatedAt { get; set; }
    [BsonField("displayOrder")] public int DisplayOrder { get; set; }
    [BsonField("v")] public int Version { get; set; }
}
