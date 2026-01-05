namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat;

public interface IFiatHistoricalDataProvider
{
    bool RequiresApiKey { get; }

    Task<IEnumerable<FiatPriceData>> GetPricesAsync(DateOnly startDate, DateOnly endDate, IEnumerable<string> currencies);

    public record FiatPriceData(DateOnly Date, IReadOnlySet<CurrencyAndPrice> Data);
    public record CurrencyAndPrice(string Currency, decimal Price);
}