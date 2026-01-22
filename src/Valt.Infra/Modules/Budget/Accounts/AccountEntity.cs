using LiteDB;

namespace Valt.Infra.Modules.Budget.Accounts;

public class AccountEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("tp")] public int AccountEntityTypeId { get; set; }
    [BsonField("name")] public string Name { get; set; } = null!;
    [BsonField("nick")] public string? CurrencyNickname { get; set; }
    [BsonField("visi")] public bool Visible { get; set; }
    [BsonField("icon")] public string? Icon { get; set; }
    [BsonField("curr")] public string? Currency { get; set; }
    [BsonField("val")] public decimal InitialAmount { get; set; }
    [BsonField("ord")] public int DisplayOrder { get; set; }
    [BsonField("grp")] public ObjectId? GroupId { get; set; }
    [BsonField("v")] public int Version { get; set; }

    [BsonIgnore]
    public AccountEntityType AccountEntityType
    {
        get => (AccountEntityType)AccountEntityTypeId;
        set => AccountEntityTypeId = (int)value;
    }
}