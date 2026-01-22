using LiteDB;

namespace Valt.Infra.Modules.Budget.Accounts;

public class AccountGroupEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("name")] public string Name { get; set; } = null!;
    [BsonField("ord")] public int DisplayOrder { get; set; }
    [BsonField("v")] public int Version { get; set; }
}
