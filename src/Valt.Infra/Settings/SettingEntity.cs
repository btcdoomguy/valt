using LiteDB;

namespace Valt.Infra.Settings;

public class SettingEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("prop")] public string Property { get; set; } = null!;
    [BsonField("value")] public string Value { get; set; } = null!;
}