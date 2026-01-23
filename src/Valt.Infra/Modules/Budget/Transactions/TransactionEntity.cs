using LiteDB;

namespace Valt.Infra.Modules.Budget.Transactions;

public class TransactionEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;

    [BsonField("type")] public TransactionEntityType Type { get; set; }
    [BsonField("date")] public DateTime Date { get; set; }
    [BsonField("name")] public string Name { get; set; } = null!;

    [BsonField("catId")] public ObjectId CategoryId { get; set; } = null!;

    [BsonField("oAccId")] public ObjectId FromAccountId { get; set; } = null!;

    [BsonField("tAccId")] public ObjectId? ToAccountId { get; set; }
    [BsonField("oFiat")] public decimal? FromFiatAmount { get; set; }
    [BsonField("oSat")] public long? FromSatAmount { get; set; }
    [BsonField("tFiat")] public decimal? ToFiatAmount { get; set; }
    [BsonField("tSat")] public long? ToSatAmount { get; set; }

    //btc price in the current fiat from the transaction account origin if BTC -> Fiat or Fiat -> BTC
    [BsonField("bPrice")] public decimal? BtcPrice { get; set; }
    [BsonField("auto")] public bool? IsAutoSatAmount { get; set; }
    [BsonField("sats")] public long? SatAmount { get; set; }
    [BsonField("sState")] public int? SatAmountStateId { get; set; }
    [BsonField("note")] public string? Notes { get; set; }

    [BsonField("gId")] public ObjectId? GroupId { get; set; }

    [BsonField("v")] public int Version { get; set; }
}