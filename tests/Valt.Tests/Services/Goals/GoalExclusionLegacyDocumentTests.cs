using System.Reflection;
using LiteDB;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Infra.Settings;
using Valt.Tests.Builders;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Valt.Tests.Services.Goals;

/// <summary>
/// GAP-1 characterization fixture: transactions persisted BEFORE phase 53 are LiteDB documents
/// WITHOUT the "excl" BSON field. The engine-side predicate shape !x.IsExcluded translates to
/// ($.excl = false), and NULL = false evaluates false — so every legacy document is silently
/// dropped from every goal query. These tests insert raw legacy-shaped documents (mapper output
/// with "excl" stripped) and pin the expected behavior: legacy docs count as non-excluded,
/// only genuinely excluded docs are dropped. Against the negation shape the legacy assertions
/// FAIL — that failure IS the GAP-1 diagnosis artifact.
/// </summary>
[TestFixture]
public class GoalExclusionLegacyDocumentTests : DatabaseTest
{
    private AccountEntity _usdAccount = null!;
    private AccountEntity _brlAccount = null!;
    private AccountEntity _btcAccount = null!;

    private StackBitcoinProgressCalculator _stackCalculator = null!;
    private GoalTransactionReader _reader = null!;

    [SetUp]
    public new void SetUp()
    {
        _stackCalculator = new StackBitcoinProgressCalculator(_localDatabase);
        _reader = new GoalTransactionReader(
            _localDatabase,
            _priceDatabase,
            new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>()));

        // Clear transactions before each test (accounts seeded once in SeedDatabase survive)
        _localDatabase.GetTransactions().DeleteAll();
    }

    protected override async Task SeedDatabase()
    {
        _usdAccount = FiatAccountBuilder.AnAccount()
            .WithName("Checking USD")
            .WithFiatCurrency(FiatCurrency.Usd)
            .Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        _brlAccount = FiatAccountBuilder.AnAccount()
            .WithName("Checking BRL")
            .WithFiatCurrency(FiatCurrency.Brl)
            .Build();
        _localDatabase.GetAccounts().Insert(_brlAccount);

        _btcAccount = BtcAccountBuilder.AnAccount()
            .WithName("Savings BTC")
            .Build();
        _localDatabase.GetAccounts().Insert(_btcAccount);

        await base.SeedDatabase();
    }

    [Test]
    public async Task StackBitcoin_Legacy_Purchase_Contributes_And_Only_Flagged_Doc_Is_Dropped()
    {
        // Arrange: one legacy purchase (no excl field), one modern unflagged, one modern excluded
        var goalTypeJson = SerializeStackBitcoin(1_000_000);
        var input = StackBitcoinInput(goalTypeJson);

        var usdId = new AccountId(_usdAccount.Id.ToString());
        var btcId = new AccountId(_btcAccount.Id.ToString());

        InsertLegacyDocument(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 5))
            .WithName("Legacy Purchase")
            .AsBitcoinPurchase(usdId, btcId, 300_000, 500m)
            .Build());

        _localDatabase.GetTransactions().Insert(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 15))
            .WithName("Modern Purchase")
            .AsBitcoinPurchase(usdId, btcId, 200_000, 400m)
            .Build());

        _localDatabase.GetTransactions().Insert(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 20))
            .WithName("Excluded Purchase")
            .AsBitcoinPurchase(usdId, btcId, 400_000, 600m)
            .WithExcluded(true)
            .Build());

        // Act
        var result = await _stackCalculator.CalculateProgressAsync(input);
        var rows = await _stackCalculator.GetContributingTransactionsAsync(input);

        // Assert: legacy + modern-unflagged = 500,000 sats; only the flagged doc is dropped
        var calculatedSats = ((StackBitcoinGoalType)result.UpdatedGoalType).CalculatedSats;
        Assert.That(calculatedSats, Is.EqualTo(500_000L));
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows!.Select(r => r.Transaction.Name),
                Is.EqualTo(new[] { "Legacy Purchase", "Modern Purchase" }));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(500_000m));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(calculatedSats));
        });
    }

    [Test]
    public async Task IncomeFiat_Reader_Legacy_Income_Contributes_And_Excluded_Doc_Is_Dropped()
    {
        // Arrange: reader-based strategy — legacy fiat income doc, modern income, modern excluded
        var goalTypeJson = JsonSerializer.Serialize(new { targetAmount = 2000m, calculatedIncome = 0m });
        var input = new GoalProgressInput(
            GoalTypeNames.IncomeFiat,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        var usdId = new AccountId(_usdAccount.Id.ToString());

        InsertLegacyDocument(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 5))
            .WithName("Legacy Salary")
            .AsFiatIncome(usdId, 600m)
            .Build());

        _localDatabase.GetTransactions().Insert(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 15))
            .WithName("Modern Salary")
            .AsFiatIncome(usdId, 400m)
            .Build());

        _localDatabase.GetTransactions().Insert(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 20))
            .WithName("Excluded Income")
            .AsFiatIncome(usdId, 500m)
            .WithExcluded(true)
            .Build());

        // Act
        var calculator = new IncomeFiatProgressCalculator(_reader);
        var result = await calculator.CalculateProgressAsync(input);
        var rows = _reader.GetIncomeRows(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        // Assert: legacy + modern income = 1,000 of 2,000 target; excluded doc absent everywhere
        var calculatedIncome = ((IncomeFiatGoalType)result.UpdatedGoalType).CalculatedIncome;
        Assert.That(calculatedIncome, Is.EqualTo(1000m));
        Assert.That(result.Progress, Is.EqualTo(50m));
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(r => r.Transaction.Name),
                Is.EqualTo(new[] { "Legacy Salary", "Modern Salary" }));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(1000m));
            Assert.That(rows[1].RunningTotal, Is.EqualTo(calculatedIncome));
        });
    }

    [Test]
    public async Task StackBitcoin_Real_Data_Mix_Of_Legacy_Shapes_All_Contribute_When_Unflagged()
    {
        // Arrange: the real-data shapes phase-53 fixtures lacked — a foreign-currency (BRL)
        // fiat leg on a GroupId-bearing (installment-style) legacy row, a plain legacy purchase,
        // a sat-only legacy Bitcoin income row — plus one modern excluded purchase.
        var goalTypeJson = SerializeStackBitcoin(1_000_000);
        var input = StackBitcoinInput(goalTypeJson);

        var brlId = new AccountId(_brlAccount.Id.ToString());
        var usdId = new AccountId(_usdAccount.Id.ToString());
        var btcId = new AccountId(_btcAccount.Id.ToString());

        InsertLegacyDocument(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 5))
            .WithName("Installment Purchase")
            .WithGroupId(new GroupId())
            .AsBitcoinPurchase(brlId, btcId, 100_000, 2500m)
            .Build());

        InsertLegacyDocument(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 10))
            .WithName("Plain Legacy Purchase")
            .AsBitcoinPurchase(usdId, btcId, 150_000, 750m)
            .Build());

        InsertLegacyDocument(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 12))
            .WithName("Legacy BTC Income")
            .AsBitcoinIncome(btcId, 50_000)
            .Build());

        _localDatabase.GetTransactions().Insert(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 20))
            .WithName("Excluded Purchase")
            .AsBitcoinPurchase(usdId, btcId, 999_000, 5000m)
            .WithExcluded(true)
            .Build());

        // Act
        var result = await _stackCalculator.CalculateProgressAsync(input);

        // Assert: 100k + 150k + 50k = 300k net; the predicate is the only defect, not row shape
        var calculatedSats = ((StackBitcoinGoalType)result.UpdatedGoalType).CalculatedSats;
        Assert.That(calculatedSats, Is.EqualTo(300_000L));
        Assert.That(result.Progress, Is.EqualTo(30m));
    }

    [Test]
    public async Task StackBitcoin_Modern_Doc_With_Explicit_Excl_False_Is_Counted()
    {
        // Arrange: a modern document with the excl field PHYSICALLY PRESENT and false —
        // guards against an over-correction that drops everything instead of only flagged docs.
        var goalTypeJson = SerializeStackBitcoin(1_000_000);
        var input = StackBitcoinInput(goalTypeJson);

        var usdId = new AccountId(_usdAccount.Id.ToString());
        var btcId = new AccountId(_btcAccount.Id.ToString());

        InsertRawDocumentWithExplicitFlag(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 5))
            .WithName("Explicit False Purchase")
            .AsBitcoinPurchase(usdId, btcId, 250_000, 1250m)
            .Build(), excluded: false);

        // Act
        var result = await _stackCalculator.CalculateProgressAsync(input);

        // Assert: excl:false keeps the row under BOTH the old and the null-safe predicate
        var calculatedSats = ((StackBitcoinGoalType)result.UpdatedGoalType).CalculatedSats;
        Assert.That(calculatedSats, Is.EqualTo(250_000L));
        Assert.That(result.Progress, Is.EqualTo(25m));
    }

    private static string SerializeStackBitcoin(long targetSats, long calculatedSats = 0)
    {
        return JsonSerializer.Serialize(new { targetSats, calculatedSats });
    }

    private static GoalProgressInput StackBitcoinInput(string goalTypeJson)
    {
        return new GoalProgressInput(
            GoalTypeNames.StackBitcoin,
            goalTypeJson,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));
    }

    /// <summary>
    /// Inserts a transaction document directly into the raw LiteDB collection with the "excl"
    /// field stripped, simulating a document written before the exclusion flag existed.
    /// Uses the app's own BsonMapper so field names match production serialization.
    /// (Pattern mirrored from TransactionRepository_LegacyDocumentTests.)
    /// </summary>
    private void InsertLegacyDocument(TransactionEntity entity)
    {
        var database = GetUnderlyingDatabase();

        var document = database.Mapper.ToDocument(entity);
        document.Remove("excl");

        database.GetCollection("budget_transactions").Insert(document);
    }

    /// <summary>
    /// Inserts a modern-shaped raw document with the "excl" field physically present and set
    /// to the given value — the complement of the legacy helper for pin-pointing document shape.
    /// </summary>
    private void InsertRawDocumentWithExplicitFlag(TransactionEntity entity, bool excluded)
    {
        var database = GetUnderlyingDatabase();

        var document = database.Mapper.ToDocument(entity);
        document["excl"] = excluded;

        database.GetCollection("budget_transactions").Insert(document);
    }

    private LiteDatabase GetUnderlyingDatabase()
    {
        var field = typeof(LocalDatabase).GetField("_database",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (LiteDatabase)field!.GetValue(_localDatabase)!;
    }
}
