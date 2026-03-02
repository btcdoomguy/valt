namespace Valt.Infra.Crawlers.Indicators;

public class IndicatorSnapshotEntity
{
    public string Id { get; set; } = "latest";
    public DateTime LastUpdatedUtc { get; set; }

    // Mayer Multiple
    public decimal? MayerMultiple_Multiple { get; set; }
    public decimal? MayerMultiple_Price { get; set; }
    public decimal? MayerMultiple_Ma200 { get; set; }

    // Rainbow Chart
    public string? RainbowChart_Zone { get; set; }
    public decimal? RainbowChart_Price { get; set; }

    // Fear & Greed
    public int? FearAndGreed_Value { get; set; }
    public string? FearAndGreed_Classification { get; set; }

    // Bitcoin Dominance
    public decimal? BitcoinDominance_Percent { get; set; }

    public IndicatorSnapshot ToSnapshot(bool isUpToDate)
    {
        return new IndicatorSnapshot
        {
            LastUpdatedUtc = LastUpdatedUtc,
            IsUpToDate = isUpToDate,
            MayerMultiple = MayerMultiple_Multiple.HasValue
                ? new MayerMultipleData(MayerMultiple_Multiple.Value, MayerMultiple_Price ?? 0, MayerMultiple_Ma200 ?? 0)
                : null,
            RainbowChart = RainbowChart_Zone is not null
                ? new RainbowChartData(RainbowChart_Zone, RainbowChart_Price ?? 0)
                : null,
            FearAndGreed = FearAndGreed_Value.HasValue
                ? new FearAndGreedData(FearAndGreed_Value.Value, FearAndGreed_Classification ?? "Unknown")
                : null,
            BitcoinDominance = BitcoinDominance_Percent.HasValue
                ? new BitcoinDominanceData(BitcoinDominance_Percent.Value)
                : null
        };
    }

    public static IndicatorSnapshotEntity FromSnapshot(IndicatorSnapshot snapshot)
    {
        return new IndicatorSnapshotEntity
        {
            Id = "latest",
            LastUpdatedUtc = snapshot.LastUpdatedUtc,
            MayerMultiple_Multiple = snapshot.MayerMultiple?.Multiple,
            MayerMultiple_Price = snapshot.MayerMultiple?.Price,
            MayerMultiple_Ma200 = snapshot.MayerMultiple?.Ma200,
            RainbowChart_Zone = snapshot.RainbowChart?.CurrentZone,
            RainbowChart_Price = snapshot.RainbowChart?.Price,
            FearAndGreed_Value = snapshot.FearAndGreed?.Value,
            FearAndGreed_Classification = snapshot.FearAndGreed?.Classification,
            BitcoinDominance_Percent = snapshot.BitcoinDominance?.DominancePercent
        };
    }
}
