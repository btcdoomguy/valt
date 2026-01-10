using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers;

namespace Valt.Tests.HistoricPriceCrawlers;

[TestFixture]
public class FrankfurterFiatHistoricalProviderTests
{
    [Test]
    public async Task Should_Get_Prices()
    {
        var frankfurterFiatHistoricalRateProvider = new FrankfurterFiatHistoricalDataProvider(new NullLogger<FrankfurterFiatHistoricalDataProvider>());
        var currencies = new[] { FiatCurrency.Brl, FiatCurrency.Eur };

        var prices = (await frankfurterFiatHistoricalRateProvider.GetPricesAsync(DateOnly.Parse("2024-1-1"), DateOnly.Parse("2024-12-31"), currencies)).ToList();

        Assert.That(prices.Count, Is.EqualTo(257));
    }
}