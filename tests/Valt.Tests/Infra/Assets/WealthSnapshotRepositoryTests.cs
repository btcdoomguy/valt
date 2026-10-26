using LiteDB;
using Valt.Infra.Modules.Assets;

namespace Valt.Tests.Infrastructure.Assets;

[TestFixture]
public class WealthSnapshotRepositoryTests : DatabaseTest
{
    private IWealthSnapshotRepository _repository = null!;

    [SetUp]
    public new void SetUp()
    {
        _repository = new WealthSnapshotRepository(_localDatabase);
        _localDatabase.GetWealthSnapshots().DeleteAll();
    }

    [Test]
    public async Task SaveAsync_Then_GetByDayAsync_Should_RoundTrip_All_Fields()
    {
        var entity = new WealthSnapshotEntity
        {
            Id = ObjectId.NewObjectId(),
            SnapshotDateTimeUtc = new DateTime(2024, 1, 15, 12, 30, 45, DateTimeKind.Utc),
            SnapshotDay = new DateOnly(2024, 1, 15),
            TotalValueInMainCurrency = 1234.56m,
            TotalValueInSats = 2469120L,
            MainCurrencyCode = "USD"
        };

        await _repository.SaveAsync(entity);

        var stored = await _repository.GetByDayAsync(new DateOnly(2024, 1, 15));
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Id, Is.EqualTo(entity.Id));
        Assert.That(stored.SnapshotDateTimeUtc, Is.EqualTo(entity.SnapshotDateTimeUtc));
        Assert.That(stored.SnapshotDay, Is.EqualTo(entity.SnapshotDay));
        Assert.That(stored.TotalValueInMainCurrency, Is.EqualTo(1234.56m));
        Assert.That(stored.TotalValueInSats, Is.EqualTo(2469120L));
        Assert.That(stored.MainCurrencyCode, Is.EqualTo("USD"));
    }

    [Test]
    public async Task SaveAsync_Twice_Same_Day_Should_Leave_Exactly_One_Row_With_Second_Value_Winning()
    {
        var day = new DateOnly(2024, 2, 10);

        await _repository.SaveAsync(new WealthSnapshotEntity
        {
            Id = ObjectId.NewObjectId(),
            SnapshotDateTimeUtc = new DateTime(2024, 2, 10, 8, 0, 0, DateTimeKind.Utc),
            SnapshotDay = day,
            TotalValueInMainCurrency = 100m,
            TotalValueInSats = 200000L,
            MainCurrencyCode = "USD"
        });

        await _repository.SaveAsync(new WealthSnapshotEntity
        {
            Id = ObjectId.NewObjectId(),
            SnapshotDateTimeUtc = new DateTime(2024, 2, 10, 18, 0, 0, DateTimeKind.Utc),
            SnapshotDay = day,
            TotalValueInMainCurrency = 250m,
            TotalValueInSats = 500000L,
            MainCurrencyCode = "USD"
        });

        var all = await _repository.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(1));

        var stored = await _repository.GetByDayAsync(day);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.TotalValueInMainCurrency, Is.EqualTo(250m));
        Assert.That(stored.TotalValueInSats, Is.EqualTo(500000L));
        Assert.That(stored.SnapshotDateTimeUtc, Is.EqualTo(new DateTime(2024, 2, 10, 18, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public async Task SaveAsync_On_Different_Days_Should_Create_One_Row_Per_Day()
    {
        await _repository.SaveAsync(new WealthSnapshotEntity
        {
            Id = ObjectId.NewObjectId(),
            SnapshotDateTimeUtc = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
            SnapshotDay = new DateOnly(2024, 3, 1),
            TotalValueInMainCurrency = 100m,
            TotalValueInSats = 200000L,
            MainCurrencyCode = "USD"
        });

        await _repository.SaveAsync(new WealthSnapshotEntity
        {
            Id = ObjectId.NewObjectId(),
            SnapshotDateTimeUtc = new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc),
            SnapshotDay = new DateOnly(2024, 3, 2),
            TotalValueInMainCurrency = 110m,
            TotalValueInSats = 220000L,
            MainCurrencyCode = "USD"
        });

        var all = await _repository.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(2));
    }
}
