namespace Valt.Core.Modules.AvgPrice;

public record AvgPriceAsset(string Name, int Precision)
{
    public static AvgPriceAsset Bitcoin => new("BTC", 8);
}