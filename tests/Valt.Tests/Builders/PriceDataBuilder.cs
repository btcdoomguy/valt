using Valt.Infra.DataAccess;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Tests.Builders;

public static class PriceDataBuilder
{
    public static void SeedRange(IPriceDatabase priceDatabase, DateTime from, DateTime to, decimal btcPriceUsd, params (string CurrencyCode, decimal Price)[] fiatRates)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity { Date = date, Price = btcPriceUsd });
            foreach (var (currencyCode, price) in fiatRates)
                priceDatabase.GetFiatData().Insert(new FiatDataEntity { Date = date, Currency = currencyCode, Price = price });
        }
    }
}
