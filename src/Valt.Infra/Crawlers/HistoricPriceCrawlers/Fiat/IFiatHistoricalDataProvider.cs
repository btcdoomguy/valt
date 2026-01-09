using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat;

public interface IFiatHistoricalDataProvider
{
    string Name { get; }
    bool RequiresApiKey { get; }
    bool InitialDownloadSource { get; }
    IReadOnlySet<FiatCurrency> SupportedCurrencies { get; }

    Task<IEnumerable<FiatPriceData>> GetPricesAsync(DateOnly startDate, DateOnly endDate, IEnumerable<FiatCurrency> currencies);

    public record FiatPriceData(DateOnly Date, IReadOnlySet<CurrencyAndPrice> Data);
    public record CurrencyAndPrice(FiatCurrency Currency, decimal Price);
}