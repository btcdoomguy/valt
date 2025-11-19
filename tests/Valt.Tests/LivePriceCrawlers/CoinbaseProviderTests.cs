using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Kernel.Time;

namespace Valt.Tests.LivePriceCrawlers;

[TestFixture]
public class CoinbaseProviderTests
{
    [Test]
    public async Task Should_Get_Prices()
    {
        var coinbaseProvider = new CoinbaseProvider(new Clock(), new NullLogger<CoinbaseProvider>());

        var prices = await coinbaseProvider.GetAsync();

        Assert.That(prices.Items.Count, Is.GreaterThan(0));
    }
}