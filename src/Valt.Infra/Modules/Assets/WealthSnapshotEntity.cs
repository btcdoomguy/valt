using LiteDB;

namespace Valt.Infra.Modules.Assets;

/// <summary>
/// Daily snapshot of the aggregate "Included in Wealth Totals" asset value
/// (the same value displayed in the Transactions tab "Assets Total" bar).
/// Infra-only storage record — the snapshot has no domain behavior.
/// At most one row per local calendar day; a same-day write overwrites the existing row.
/// </summary>
public class WealthSnapshotEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("dt")] public DateTime SnapshotDateTimeUtc { get; set; }
    [BsonField("day")] public DateOnly SnapshotDay { get; set; }
    [BsonField("fiat")] public decimal TotalValueInMainCurrency { get; set; }
    [BsonField("sats")] public long TotalValueInSats { get; set; }
    [BsonField("cur")] public string MainCurrencyCode { get; set; } = null!;
}
