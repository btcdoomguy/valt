using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers;

public interface ILocalHistoricalPriceProvider
{
    Task<decimal?> GetFiatRateAtAsync(DateOnly date, FiatCurrency currency);
    Task<decimal?> GetUsdBitcoinRateAtAsync(DateOnly date);
    Task<IEnumerable<FiatRate>> GetAllFiatRatesAtAsync(DateOnly fiatLastDateStored);

    public record FiatRate(FiatCurrency Currency, decimal Rate, DateOnly Date);
}