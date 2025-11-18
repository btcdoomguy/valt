using LiteDB;

namespace Valt.Infra.Modules.Configuration;

public class ConfigurationEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("key")] public string Key { get; set; } = null!;
    [BsonField("value")] public string Value { get; set; } = null!;
}