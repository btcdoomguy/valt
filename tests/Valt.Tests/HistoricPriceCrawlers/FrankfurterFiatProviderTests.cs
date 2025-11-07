using Microsoft.Extensions.Logging.Abstractions;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers;

namespace Valt.Tests.HistoricPriceCrawlers;

[TestFixture]
public class FrankfurterFiatHistoricalProviderTests
{
    [Test]
    public async Task Should_Get_Prices()
    {
        var frankfurterFiatHistoricalRateProvider = new FrankfurterFiatHistoricalDataProvider(new NullLogger<FrankfurterFiatHistoricalDataProvider>());

        var prices =(await frankfurterFiatHistoricalRateProvider.GetPricesAsync(DateOnly.Parse("2024-1-1"), DateOnly.Parse("2024-12-31"))).ToList();

        Assert.That(prices.Count, Is.EqualTo(257));
    }
}