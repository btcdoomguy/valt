using LiteDB;

namespace Valt.Infra.Modules.DataSources.Fiat;

public class FiatDataEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("dt")] public DateTime Date { get; set; }
    [BsonField("c")] public required string Currency { get; set; }
    [BsonField("v")] public decimal Price { get; set; }
}