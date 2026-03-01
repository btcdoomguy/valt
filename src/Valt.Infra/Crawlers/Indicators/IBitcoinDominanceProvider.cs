namespace Valt.Infra.Crawlers.Indicators;

public interface IBitcoinDominanceProvider
{
    Task<BitcoinDominanceData> GetAsync();
}
