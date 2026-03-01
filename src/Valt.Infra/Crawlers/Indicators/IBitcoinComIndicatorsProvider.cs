namespace Valt.Infra.Crawlers.Indicators;

public interface IBitcoinComIndicatorsProvider
{
    Task<MayerMultipleData> GetMayerMultipleAsync();
    Task<RainbowChartData> GetRainbowChartAsync();
    Task<PiCycleTopData> GetPiCycleTopAsync();
    Task<StockToFlowData> GetStockToFlowAsync();
}
