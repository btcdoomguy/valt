using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat;

public interface IFiatHistoricalDataProvider
{
    string Name { get; }
    bool RequiresApiKey { get; }
    bool InitialDownloadSource { get; }

    /// <summary>
    /// Fallback providers are used for currencies not supported by the primary providers.
    /// When multiple fallback providers exist, the first non-fallback provider that supports
    /// the currency is preferred over fallback providers.
    /// </summary>
    bool IsFallbackProvider { get; }

    IReadOnlySet<FiatCurrency> SupportedCurrencies { get; }

    Task<IEnumerable<FiatPriceData>> GetPricesAsync(DateOnly startDate, DateOnly endDate, IEnumerable<FiatCurrency> currencies);

    public record FiatPriceData(DateOnly Date, IReadOnlySet<CurrencyAndPrice> Data);
    public record CurrencyAndPrice(FiatCurrency Currency, decimal Price);
}