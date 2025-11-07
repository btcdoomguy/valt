using LiteDB;

namespace Valt.Infra.Modules.DataSources.Bitcoin;

public class BitcoinDataEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("dt")] public DateTime Date { get; set; }
    [BsonField("v")] public decimal Price { get; set; }
}