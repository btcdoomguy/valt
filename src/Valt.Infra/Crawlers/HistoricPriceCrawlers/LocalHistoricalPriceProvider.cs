using Valt.Core.Common;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers;

public class LocalHistoricalPriceProvider : ILocalHistoricalPriceProvider
{
    private readonly IPriceDatabase _priceDatabase;

    public LocalHistoricalPriceProvider(IPriceDatabase priceDatabase)
    {
        _priceDatabase = priceDatabase;
    }

    public Task<decimal?> GetFiatRateAtAsync(DateOnly date, FiatCurrency currency)
    {
        var finalDate = date.ToValtDateTime();
        var startDate = finalDate.AddDays(-5);
        var currencyCode = currency.Code;

        var entry = _priceDatabase.GetFiatData()
            .Find(x => x.Date >= startDate && x.Date <= finalDate && x.Currency == currencyCode)
            .OrderByDescending(x => x.Date).FirstOrDefault();

        return Task.FromResult(entry?.Price);
    }

    public Task<decimal?> GetUsdBitcoinRateAtAsync(DateOnly date)
    {
        var dateToScan = date.ToValtDateTime();

        var entry = _priceDatabase.GetBitcoinData().FindOne(x => x.Date == dateToScan);

        return Task.FromResult(entry?.Price);
    }
}