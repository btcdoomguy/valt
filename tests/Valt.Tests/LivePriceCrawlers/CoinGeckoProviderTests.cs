using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Kernel.Time;

namespace Valt.Tests.LivePriceCrawlers;

[TestFixture]
public class CoinGeckoProviderTests
{
    [Test]
    public async Task Should_Get_Prices_With_Usd_And_Up_To_Date()
    {
        var provider = new CoinGeckoProvider(new Clock(), new NullLogger<CoinGeckoProvider>());

        var prices = await provider.GetAsync();

        Assert.That(prices.Items.Count, Is.GreaterThan(0));
        Assert.That(prices.UpToDate, Is.True);

        var usdPrice = prices.Items.SingleOrDefault(x => x.CurrencyCode == FiatCurrency.Usd.Code);
        Assert.That(usdPrice, Is.Not.Null);
        Assert.That(usdPrice!.Price, Is.GreaterThan(0));
    }
}
