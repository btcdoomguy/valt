using LiteDB;
using Valt.Core.Modules.AvgPrice;

namespace Valt.Infra.Modules.AvgPrice;

public class AvgPriceLineEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("profId")] public ObjectId ProfileId { get; set; } = null!;
    [BsonField("dt")] public DateTime Date { get; set; }
    [BsonField("ord")] public int DisplayOrder { get; set; }
    [BsonField("type")] public int AvgPriceLineTypeId { get; set; }
    [BsonField("qt")] public long BtcAmount { get; set; }
    [BsonField("price")] public decimal BtcUnitPrice { get; set; }
    [BsonField("notes")] public string Comment { get; set; } = null!;
    [BsonField("totalAvgCost")] public decimal AvgCostOfAcquisition { get; set; }
    [BsonField("totalCost")] public decimal TotalCost { get; set; }
    [BsonField("totalQty")] public long TotalBtcAmount { get; set; }
    
    [BsonIgnore]
    public AvgPriceLineTypes AvgPriceLineType
    {
        get => (AvgPriceLineTypes)AvgPriceLineTypeId;
        set => AvgPriceLineTypeId = (int)value;
    }
}