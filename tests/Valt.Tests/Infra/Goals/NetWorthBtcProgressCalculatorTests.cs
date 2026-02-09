using LiteDB;
using NSubstitute;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Infra.Settings;

namespace Valt.Tests.Infrastructure.Goals;

[TestFixture]
public class NetWorthBtcProgressCalculatorTests : IDisposable
{
    private ILocalDatabase _localDatabase = null!;
    private IPriceDatabase _priceDatabase = null!;
    private CurrencySettings _currencySettings = null!;
    private NetWorthBtcProgressCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _localDatabase = Substitute.For<ILocalDatabase>();
        _priceDatabase = Substitute.For<IPriceDatabase>();
        var notificationPublisher = Substitute.For<INotificationPublisher>();
        _currencySettings = new CurrencySettings(_localDatabase, notificationPublisher);
        _calculator = new NetWorthBtcProgressCalculator(_localDatabase, _priceDatabase, _currencySettings);
    }

    [TearDown]
    public void TearDown()
    {
        _localDatabase?.Dispose();
        _priceDatabase?.Dispose();
    }

    public void Dispose()
    {
        TearDown();
    }

    [Test]
    public void SupportedType_IsNetWorthBtc()
    {
        Assert.That(_calculator.SupportedType, Is.EqualTo(GoalTypeNames.NetWorthBtc));
    }

    [Test]
    public async Task CalculateProgress_WithBitcoinAccount_CalculatesSatsCorrectly()
    {
        var goalType = new NetWorthBtcGoalType(10_000_000L);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.NetWorthBtc, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        var accountId = ObjectId.NewObjectId();
        var accounts = new List<AccountEntity>
        {
            new() { Id = accountId, AccountEntityTypeId = (int)AccountEntityType.Bitcoin, Visible = true }
        };
        var caches = new List<AccountCacheEntity>
        {
            new() { Id = accountId, CurrentTotal = 5_000_000 }
        };

        SetupDatabase(accounts, caches, 50000m, new Dictionary<string, decimal>());

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(50m));
        var updated = (NetWorthBtcGoalType)result.UpdatedGoalType;
        Assert.That(updated.CalculatedSats, Is.EqualTo(5_000_000L));
    }

    [Test]
    public async Task CalculateProgress_WithNoAccounts_ReturnsZeroProgress()
    {
        var goalType = new NetWorthBtcGoalType(10_000_000L);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.NetWorthBtc, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        SetupDatabase([], [], 50000m, new Dictionary<string, decimal>());

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    [Test]
    public async Task CalculateProgress_SkipsInvisibleAccounts()
    {
        var goalType = new NetWorthBtcGoalType(10_000_000L);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.NetWorthBtc, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        var accountId = ObjectId.NewObjectId();
        var accounts = new List<AccountEntity>
        {
            new() { Id = accountId, AccountEntityTypeId = (int)AccountEntityType.Bitcoin, Visible = false }
        };
        var caches = new List<AccountCacheEntity>
        {
            new() { Id = accountId, CurrentTotal = 5_000_000 }
        };

        SetupDatabase(accounts, caches, 50000m, new Dictionary<string, decimal>());

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(0m));
    }

    private void SetupDatabase(
        List<AccountEntity> accounts,
        List<AccountCacheEntity> caches,
        decimal btcPrice,
        Dictionary<string, decimal> fiatRates)
    {
        var accountCollection = Substitute.For<ILiteCollection<AccountEntity>>();
        accountCollection.FindAll().Returns(accounts);
        _localDatabase.GetAccounts().Returns(accountCollection);

        var cacheCollection = Substitute.For<ILiteCollection<AccountCacheEntity>>();
        cacheCollection.FindAll().Returns(caches);
        _localDatabase.GetAccountCaches().Returns(cacheCollection);

        var btcData = btcPrice > 0
            ? new List<BitcoinDataEntity> { new() { Date = DateTime.UtcNow, Price = btcPrice } }
            : new List<BitcoinDataEntity>();
        var btcCollection = Substitute.For<ILiteCollection<BitcoinDataEntity>>();
        btcCollection.FindAll().Returns(btcData);
        _priceDatabase.GetBitcoinData().Returns(btcCollection);

        var fiatData = fiatRates.Select(kvp => new FiatDataEntity
        {
            Currency = kvp.Key,
            Date = DateTime.UtcNow,
            Price = kvp.Value
        }).ToList();
        var fiatCollection = Substitute.For<ILiteCollection<FiatDataEntity>>();
        fiatCollection.FindAll().Returns(fiatData);
        _priceDatabase.GetFiatData().Returns(fiatCollection);
    }
}
