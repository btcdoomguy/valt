using LiteDB;

namespace Valt.Infra.Modules.Budget.Transactions;

public class TransactionTermEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("name")] public string Name { get; set; } = null!;
    [BsonField("catId")] public ObjectId CategoryId { get; set; } = null!;
    [BsonField("sat")] public long? SatAmount { get; set; }
    [BsonField("fiat")] public decimal? FiatAmount { get; set; }
    [BsonField("count")] public int Count { get; set; }
}