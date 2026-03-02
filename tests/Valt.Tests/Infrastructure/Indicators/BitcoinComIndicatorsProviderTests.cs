using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Infra.Crawlers.Indicators;

namespace Valt.Tests.Infrastructure.Indicators;

[TestFixture]
public class BitcoinComIndicatorsProviderTests
{
    private BitcoinComIndicatorsProvider _provider = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _provider = new BitcoinComIndicatorsProvider(
            Substitute.For<ILogger<BitcoinComIndicatorsProvider>>());
    }

    [Test]
    public async Task GetMayerMultipleAsync_ReturnsValidData()
    {
        var data = await _provider.GetMayerMultipleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(data.Multiple, Is.GreaterThan(0));
            Assert.That(data.Price, Is.GreaterThan(0));
            Assert.That(data.Ma200, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task GetRainbowChartAsync_ReturnsValidData()
    {
        var data = await _provider.GetRainbowChartAsync();

        Assert.Multiple(() =>
        {
            Assert.That(data.CurrentZone, Is.Not.Null.And.Not.Empty);
            Assert.That(data.Price, Is.GreaterThan(0));
        });
    }

}
