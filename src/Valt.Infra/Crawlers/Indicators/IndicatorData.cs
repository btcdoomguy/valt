namespace Valt.Infra.Crawlers.Indicators;

public record IndicatorSnapshot
{
    public DateTime LastUpdatedUtc { get; init; }
    public bool IsUpToDate { get; init; }
    public MayerMultipleData? MayerMultiple { get; init; }
    public RainbowChartData? RainbowChart { get; init; }
    public PiCycleTopData? PiCycleTop { get; init; }
    public StockToFlowData? StockToFlow { get; init; }
    public FearAndGreedData? FearAndGreed { get; init; }
    public BitcoinDominanceData? BitcoinDominance { get; init; }
}

public record MayerMultipleData(decimal Multiple, decimal Price, decimal Ma200);

public record RainbowChartData(string CurrentZone, decimal Price);

public record PiCycleTopData(decimal Ma111, decimal Ma350x2, decimal Price, bool IsConverging);

public record StockToFlowData(decimal ModelPrice, decimal ActualPrice, decimal Ratio);

public record FearAndGreedData(int Value, string Classification);

public record BitcoinDominanceData(decimal DominancePercent);
