namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;

public interface IBitcoinHistoricalDataProvider
{
    bool RequiresApiKey { get; }

    Task<IEnumerable<BitcoinPriceData>> GetPricesAsync(DateOnly startDate, DateOnly endDate);

    public record BitcoinPriceData(DateOnly Date, decimal Price);
}