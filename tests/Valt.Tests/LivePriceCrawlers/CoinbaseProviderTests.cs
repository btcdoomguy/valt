using Microsoft.Extensions.Logging.Abstractions;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

namespace Valt.Tests.LivePriceCrawlers;

[TestFixture]
public class CoinbaseProviderTests
{
    [Test]
    public async Task Should_Get_Prices()
    {
        var coinbaseProvider = new CoinbaseProvider(new NullLogger<CoinbaseProvider>());

        var prices = await coinbaseProvider.GetAsync();

        //tests will fail if btc dies :(
        Assert.That(prices.SingleOrDefault(x => x.CurrencyCode == "USD")!.Price, Is.GreaterThan(0));
        Assert.That(prices.SingleOrDefault(x => x.CurrencyCode == "EUR")!.Price, Is.GreaterThan(0));
        Assert.That(prices.SingleOrDefault(x => x.CurrencyCode == "BRL")!.Price, Is.GreaterThan(0));
    }
}