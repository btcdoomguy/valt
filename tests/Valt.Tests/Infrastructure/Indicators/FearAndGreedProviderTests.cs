using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Infra.Crawlers.Indicators;

namespace Valt.Tests.Infrastructure.Indicators;

[TestFixture]
public class FearAndGreedProviderTests
{
    private FearAndGreedProvider _provider = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _provider = new FearAndGreedProvider(
            Substitute.For<ILogger<FearAndGreedProvider>>());
    }

    [Test]
    public async Task GetAsync_ReturnsValidData()
    {
        var data = await _provider.GetAsync();

        Assert.Multiple(() =>
        {
            Assert.That(data.Value, Is.InRange(0, 100));
            Assert.That(data.Classification, Is.Not.Null.And.Not.Empty);
        });
    }
}
