using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers;

public interface ILocalHistoricalPriceProvider
{
    Task<decimal?> GetFiatRateAtAsync(DateOnly date, FiatCurrency currency);
    Task<decimal?> GetUsdBitcoinRateAtAsync(DateOnly date);
}