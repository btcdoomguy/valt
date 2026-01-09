using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Valt.Infra;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;
using Valt.Infra.Modules.DataSources.Bitcoin;

namespace Valt.Tests.HistoricPriceCrawlers;

[TestFixture]
public class FillLocalDbWithDataTests : DatabaseTest
{
    protected override async Task SeedDatabase()
    {
        if (_priceDatabase.GetBitcoinData().Query().Count() == 0)
        {
            var provider = new BitcoinInitialSeedPriceProvider(NullLogger<BitcoinInitialSeedPriceProvider>.Instance);
            var prices = await provider.GetPricesAsync();
            _priceDatabase.GetBitcoinData().InsertBulk(prices.Select(x => new BitcoinDataEntity()
            {
                Date = x.Date.ToValtDateTime(),
                Price = x.Price
            }));
        }
    }

    [Test]
    public void Should_GetSpecificPrice_Fast()
    {
        var priceDate = new DateOnly(2018, 3, 8).ToValtDateTime();
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var entry = _priceDatabase.GetBitcoinData().Query().Where(x => x.Date == priceDate).FirstOrDefault();
        stopWatch.Stop();

        Assert.That(entry.Price, Is.EqualTo(9999.68m));
        Assert.That(stopWatch.ElapsedMilliseconds, Is.LessThan(50));
    }
}