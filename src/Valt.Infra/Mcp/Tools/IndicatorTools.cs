using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.Infra.Crawlers.Indicators;

namespace Valt.Infra.Mcp.Tools;

[McpServerToolType]
public class IndicatorTools
{
    [McpServerTool, Description("Get current Bitcoin macro indicators including Mayer Multiple, Rainbow Chart, Fear & Greed Index, and Bitcoin Dominance. Returns cached data if available.")]
    public static IndicatorResultDto GetBitcoinIndicators(IIndicatorCache indicatorCache)
    {
        var snapshot = indicatorCache.GetLatest();
        if (snapshot is null)
        {
            return new IndicatorResultDto
            {
                Available = false,
                Message = "No indicator data available yet. Data will be fetched by the background job within 2 minutes."
            };
        }

        return new IndicatorResultDto
        {
            Available = true,
            IsUpToDate = snapshot.IsUpToDate,
            LastUpdatedUtc = snapshot.LastUpdatedUtc.ToString("O"),
            MayerMultiple = snapshot.MayerMultiple is not null
                ? new MayerMultipleDto
                {
                    Multiple = snapshot.MayerMultiple.Multiple,
                    Price = snapshot.MayerMultiple.Price,
                    Ma200 = snapshot.MayerMultiple.Ma200
                }
                : null,
            RainbowChart = snapshot.RainbowChart is not null
                ? new RainbowChartDto
                {
                    CurrentZone = snapshot.RainbowChart.CurrentZone,
                    Price = snapshot.RainbowChart.Price
                }
                : null,
            FearAndGreed = snapshot.FearAndGreed is not null
                ? new FearAndGreedDto
                {
                    Value = snapshot.FearAndGreed.Value,
                    Classification = snapshot.FearAndGreed.Classification
                }
                : null,
            BitcoinDominance = snapshot.BitcoinDominance is not null
                ? new BitcoinDominanceDto
                {
                    DominancePercent = snapshot.BitcoinDominance.DominancePercent
                }
                : null
        };
    }

    #region DTOs

    public class IndicatorResultDto
    {
        public required bool Available { get; init; }
        public string? Message { get; init; }
        public bool IsUpToDate { get; init; }
        public string? LastUpdatedUtc { get; init; }
        public MayerMultipleDto? MayerMultiple { get; init; }
        public RainbowChartDto? RainbowChart { get; init; }
        public FearAndGreedDto? FearAndGreed { get; init; }
        public BitcoinDominanceDto? BitcoinDominance { get; init; }
    }

    public class MayerMultipleDto
    {
        public required decimal Multiple { get; init; }
        public required decimal Price { get; init; }
        public required decimal Ma200 { get; init; }
    }

    public class RainbowChartDto
    {
        public required string CurrentZone { get; init; }
        public required decimal Price { get; init; }
    }

    public class FearAndGreedDto
    {
        public required int Value { get; init; }
        public required string Classification { get; init; }
    }

    public class BitcoinDominanceDto
    {
        public required decimal DominancePercent { get; init; }
    }

    #endregion
}
