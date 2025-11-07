using LiteDB;

namespace Valt.Infra.Modules.Budget.Accounts;

public class AccountCacheEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("total")] public decimal Total { get; set; }
    
    [BsonField("currentTotal")] public decimal CurrentTotal { get; set; }
    [BsonField("currentDate")] public DateTime CurrentDate { get; set; }
}