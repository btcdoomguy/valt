using LiteDB;

namespace Valt.Infra.Modules.Assets;

public class AssetGroupEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("name")] public string Name { get; set; } = null!;
    [BsonField("description")] public string Description { get; set; } = string.Empty;
    [BsonField("ord")] public int DisplayOrder { get; set; }
    [BsonField("v")] public int Version { get; set; }
}
