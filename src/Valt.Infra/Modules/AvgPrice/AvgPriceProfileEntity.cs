using LiteDB;
using Valt.Core.Modules.AvgPrice;

namespace Valt.Infra.Modules.AvgPrice;

public class AvgPriceProfileEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonField("tp")] public int AvgPriceCalculationMethodId { get; set; }
    [BsonField("name")] public string Name { get; set; } = null!;
    [BsonField("assetName")] public string AssetName { get; set; } = null!;
    [BsonField("assetPrec")] public int Precision { get; set; }
    [BsonField("visi")] public bool Visible { get; set; }
    [BsonField("icon")] public string? Icon { get; set; }
    [BsonField("curr")] public string Currency { get; set; } = null!;
    
    [BsonRef("lines")]
    public List<AvgPriceLineEntity> Lines { get; set; } = new();
    
    [BsonField("v")] public int Version { get; set; }

    [BsonIgnore]
    public AvgPriceCalculationMethod AvgPriceCalculationMethod
    {
        get => (AvgPriceCalculationMethod)AvgPriceCalculationMethodId;
        set => AvgPriceCalculationMethodId = (int)value;
    }
}