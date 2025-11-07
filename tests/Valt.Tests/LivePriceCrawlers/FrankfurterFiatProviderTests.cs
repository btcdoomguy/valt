using Microsoft.Extensions.Logging.Abstractions;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

namespace Valt.Tests.LivePriceCrawlers;

[TestFixture]
public class FrankfurterFiatProviderTests
{
    [Test]
    public async Task Should_Get_Prices()
    {
        var frankfurterUsdRateProvider = new FrankfurterFiatRateProvider(new NullLogger<FrankfurterFiatRateProvider>());

        var prices = await frankfurterUsdRateProvider.GetAsync();

        Assert.That(prices.SingleOrDefault(x => x.CurrencyCode == "BRL")!.Price, Is.GreaterThan(0));
        Assert.That(prices.SingleOrDefault(x => x.CurrencyCode == "EUR")!.Price, Is.GreaterThan(0));
    }
}