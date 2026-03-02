namespace Valt.Infra.Crawlers.Indicators;

public record IndicatorSnapshot
{
    public DateTime LastUpdatedUtc { get; init; }
    public bool IsUpToDate { get; init; }
    public MayerMultipleData? MayerMultiple { get; init; }
    public RainbowChartData? RainbowChart { get; init; }
    public FearAndGreedData? FearAndGreed { get; init; }
    public BitcoinDominanceData? BitcoinDominance { get; init; }
}

public record MayerMultipleData(decimal Multiple, decimal Price, decimal Ma200);

public record RainbowChartData(string CurrentZone, decimal Price);

public record FearAndGreedData(int Value, string Classification);

public record BitcoinDominanceData(decimal DominancePercent);
