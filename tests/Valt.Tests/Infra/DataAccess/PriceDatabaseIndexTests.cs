using System.Reflection;
using LiteDB;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.Time;

namespace Valt.Tests.Infrastructure.DataAccess;

[TestFixture]
public class PriceDatabaseIndexTests
{
    private MemoryStream _stream = null!;
    private PriceDatabase _database = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _stream = new MemoryStream();
        _database = new PriceDatabase(new Clock(), Substitute.For<INotificationPublisher>());
        _database.OpenInMemoryDatabase(_stream);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _database.Dispose();
        _stream.Dispose();
    }

    private LiteDatabase GetUnderlyingDatabase()
    {
        var field = typeof(PriceDatabase).GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
        return (LiteDatabase)field!.GetValue(_database)!;
    }

    private List<string> GetIndexExpressions(string collectionName)
    {
        return GetUnderlyingDatabase()
            .GetCollection("$indexes")
            .Find($"collection = '{collectionName}'")
            .Select(d => d["expression"].AsString)
            .ToList();
    }

    [Test]
    public void BitcoinData_Indexes_Are_Ensured()
    {
        _database.GetBitcoinData();
        var indexes = GetIndexExpressions("datasource_bitcoin");

        Assert.That(indexes, Does.Contain("$.dt"));
    }

    [Test]
    public void FiatData_Indexes_Are_Ensured()
    {
        _database.GetFiatData();
        var indexes = GetIndexExpressions("datasource_fiat");

        Assert.That(indexes, Does.Contain("$.dt"));
        Assert.That(indexes, Does.Contain("$.c"));
    }
}
