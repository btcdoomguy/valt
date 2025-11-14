using Valt.Infra.Modules.DataSources.Bitcoin;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;

public interface IBitcoinInitialSeedPriceProvider
{
    Task<IEnumerable<BitcoinPriceData>> GetPricesAsync();
}