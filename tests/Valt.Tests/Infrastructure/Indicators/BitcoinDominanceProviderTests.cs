using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Infra.Crawlers.Indicators;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

namespace Valt.Tests.Infrastructure.Indicators;

[TestFixture]
public class BitcoinDominanceProviderTests
{
    private BitcoinDominanceProvider _provider = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _provider = new BitcoinDominanceProvider(
            HttpClientTestFactory.Create(),
            Substitute.For<ILogger<BitcoinDominanceProvider>>(),
            new CoinGeckoRateLimiter(Substitute.For<ILogger<CoinGeckoRateLimiter>>()));
    }

    [Test]
    public async Task GetAsync_ReturnsValidData()
    {
        var data = await _provider.GetAsync();

        Assert.That(data.DominancePercent, Is.InRange(1m, 100m));
    }
}
