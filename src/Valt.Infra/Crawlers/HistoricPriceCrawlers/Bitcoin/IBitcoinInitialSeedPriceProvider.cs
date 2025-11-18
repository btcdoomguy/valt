namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;

public interface IBitcoinInitialSeedPriceProvider
{
    Task<IEnumerable<BitcoinPriceData>> GetPricesAsync();
}