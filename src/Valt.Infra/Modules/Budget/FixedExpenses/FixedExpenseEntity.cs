using LiteDB;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

public class FixedExpenseEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("name")] public string Name { get; set; } = null!;
    [BsonField("catId")] public required ObjectId CategoryId { get; set; }
    [BsonField("accId")] public ObjectId? DefaultAccountId { get; set; }
    [BsonField("curr")] public string? Currency { get; set; }
    
    [BsonField("fixed")] public decimal? FixedAmount { get; set; }
    [BsonField("rangeMin")] public decimal? RangedAmountMin { get; set; }
    [BsonField("rangeMax")] public decimal? RangedAmountMax { get; set; }
    [BsonField("period")] public int PeriodId { get; set; }
    [BsonField("periodDt")] public DateTime PeriodStart { get; set; }
    [BsonField("day")] public int Day { get; set; }
    [BsonField("enabled")] public bool Enabled { get; set; }
    [BsonField("v")] public int Version { get; set; }
    
    [BsonField("ranges")] public List<FixedExpenseRangeEntity> Ranges { get; set; } = new();
}