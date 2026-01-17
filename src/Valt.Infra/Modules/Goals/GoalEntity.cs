using LiteDB;

namespace Valt.Infra.Modules.Goals;

public class GoalEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;

    [BsonField("refDate")] public DateTime RefDate { get; set; }
    [BsonField("period")] public int PeriodId { get; set; }
    [BsonField("typeName")] public int GoalTypeNameId { get; set; }
    [BsonField("typeJson")] public string GoalTypeJson { get; set; } = null!;
    [BsonField("progress")] public decimal Progress { get; set; }
    [BsonField("upToDate")] public bool IsUpToDate { get; set; }
    [BsonField("lastUpdated")] public DateTime LastUpdatedAt { get; set; }
    [BsonField("state")] public int StateId { get; set; }
    [BsonField("v")] public int Version { get; set; }
}
