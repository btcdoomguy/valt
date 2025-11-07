using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Valt.Infra.Modules.DataSources.Bitcoin;

namespace Valt.Tests.HistoricPriceCrawlers;

[TestFixture]
public class FillLocalDbWithDataTests : DatabaseTest
{
    protected override async Task SeedDatabase()
    {
        var assembly = Assembly.Load("Valt.Infra");
        var dataResource = @"Valt.Infra.Crawlers.HistoricPriceCrawlers.initial-seed-price.csv";

        var entities = new List<BitcoinDataEntity>();
        using (var stream = assembly.GetManifestResourceStream(dataResource)!)
        {
            using var reader = new StreamReader(stream);

            var nextLine = reader.ReadLine();
            decimal lastValidPrice = 0;
            while (nextLine is not null)
            {
                var split = nextLine.Split(',');

                if (split[1] != "nan")
                    lastValidPrice = decimal.Parse(split[1], CultureInfo.InvariantCulture);

                entities.Add(new BitcoinDataEntity()
                {
                    Date = DateTime.Parse(split[0]),
                    Price = lastValidPrice
                });

                nextLine = reader.ReadLine();
            }
        }

        _priceDatabase.GetBitcoinData().InsertBulk(entities, entities.Count);
    }

    [Test]
    public void Should_GetSpecificPrice_Fast()
    {
        var priceDate = new DateTime(2018, 3, 8);
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var entry = _priceDatabase.GetBitcoinData().Query().Where(x => x.Date == priceDate).FirstOrDefault();
        stopWatch.Stop();

        Assert.That(entry.Price, Is.EqualTo(9999.68m));
        Assert.That(stopWatch.ElapsedMilliseconds, Is.LessThan(50));
    }
}