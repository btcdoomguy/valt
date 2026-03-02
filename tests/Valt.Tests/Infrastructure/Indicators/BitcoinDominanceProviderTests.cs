using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Infra.Crawlers.Indicators;

namespace Valt.Tests.Infrastructure.Indicators;

[TestFixture]
public class BitcoinDominanceProviderTests
{
    private BitcoinDominanceProvider _provider = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _provider = new BitcoinDominanceProvider(
            Substitute.For<ILogger<BitcoinDominanceProvider>>());
    }

    [Test]
    public async Task GetAsync_ReturnsValidData()
    {
        var data = await _provider.GetAsync();

        Assert.That(data.DominancePercent, Is.InRange(1m, 100m));
    }
}
