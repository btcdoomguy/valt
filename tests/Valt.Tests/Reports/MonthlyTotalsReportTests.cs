using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Assets;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class MonthlyTotalsReportTests : DatabaseTest
{
    private AccountEntity _btcAccount = null!;
    private AccountEntity _usdAccount = null!;
    private AccountEntity _brlAccount = null!;
    private AccountEntity _eurAccount = null!;

    private CategoryId _categoryId = null!;

    private readonly List<ObjectId> _insertedTransactionIds = new();

    [SetUp]
    public new void SetUp()
    {
        _localDatabase.GetWealthSnapshots().DeleteAll();
    }

    [TearDown]
    public void TearDownExclusionTestTransactions()
    {
        foreach (var id in _insertedTransactionIds)
            _localDatabase.GetTransactions().Delete(id);
        _insertedTransactionIds.Clear();
    }

    protected override Task SeedDatabase()
    {
        _categoryId = IdGenerator.Generate();

        //initialize demo accounts
        _btcAccount = new BtcAccountBuilder()
        {
            Name = "BTC Account",
            Value = BtcValue.ParseBitcoin(1)
        }.Build();
        _localDatabase.GetAccounts().Insert(_btcAccount);

        _usdAccount = new FiatAccountBuilder()
        {
            Name = "USD Account",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(1000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        _brlAccount = new FiatAccountBuilder()
        {
            Name = "BRL Account",
            FiatCurrency = FiatCurrency.Brl,
            Value = FiatValue.New(1000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_brlAccount);

        _eurAccount = new FiatAccountBuilder()
        {
            Name = "EUR Account",
            FiatCurrency = FiatCurrency.Eur,
            Value = FiatValue.New(1000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_eurAccount);

        //feed some fake rates and predefined transactions

        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity()
            {
                Date = currentDate,
                Price = 100000m
            });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity()
            {
                Date = currentDate,
                Currency = FiatCurrency.Brl.Code,
                Price = 5.5m
            });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity()
            {
                Date = currentDate,
                Currency = FiatCurrency.Eur.Code,
                Price = 0.75m
            });

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "USD Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new FiatDetails(_usdAccount.Id.ToString(), 100m, true)
                    : new FiatDetails(_usdAccount.Id.ToString(), 1m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "BRL Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
                    : new FiatDetails(_brlAccount.Id.ToString(), 1m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "EUR Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new FiatDetails(_eurAccount.Id.ToString(), 100m, true)
                    : new FiatDetails(_eurAccount.Id.ToString(), 1m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "BTC Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new BitcoinDetails(_btcAccount.Id.ToString(), 10000, true)
                    : new BitcoinDetails(_btcAccount.Id.ToString(), 100, false)
            }.Build());

            currentDate = currentDate.AddDays(1);
        }

        //some transfers
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = DateOnly.FromDateTime(new DateTime(2024, 12, 01)),
            Name = "BTC to USD Transfer",
            TransactionDetails = new BitcoinToFiatDetails(_btcAccount.Id.ToString(), _usdAccount.Id.ToString(),
                BtcValue.ParseSats(10000), FiatValue.New(1000m))
        }.Build());
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = DateOnly.FromDateTime(new DateTime(2025, 1, 01)),
            Name = "USD to BTC Transfer",
            TransactionDetails = new FiatToBitcoinDetails(_usdAccount.Id.ToString(), _btcAccount.Id.ToString(),
                FiatValue.New(1000m), BtcValue.ParseSats(10000))
        }.Build());

        return base.SeedDatabase();
    }

    [Test]
    public async Task Should_Calculate_MonthlyTotals()
    {
        var baseDate = new DateOnly(2025, 12, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>(), _localDatabase);

        var result2024 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2024, 01, 01), new DateOnly(2024, 12, 31)),
            FiatCurrency.Brl, provider);
        var result2025 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 12, 31)),
            FiatCurrency.Brl, provider);

        Assert.That(result2024.MainCurrency, Is.EqualTo(FiatCurrency.Brl));
        Assert.That(result2024.Items[0].BtcTotal, Is.EqualTo(1.00007m));
        Assert.That(result2024.Items[0].FiatTotal, Is.EqualTo(564840.17m));
        Assert.That(result2024.Items[0].BtcMonthlyChange, Is.Zero);
        Assert.That(result2024.Items[0].BtcYearlyChange, Is.Zero);
        Assert.That(result2024.Items[0].Income, Is.EqualTo(1383.33m));
        Assert.That(result2024.Items[0].Expenses, Is.EqualTo(-415m));

        Assert.That(result2024.Items[11].BtcTotal, Is.EqualTo(1.000746m));
        Assert.That(result2024.Items[11].FiatTotal, Is.EqualTo(581446.63m));
        Assert.That(result2024.Items[11].BtcMonthlyChange, Is.EqualTo(0.00m));
        Assert.That(result2024.Items[11].BtcYearlyChange, Is.Zero);
        Assert.That(result2024.Items[11].Income, Is.EqualTo(1383.33m));
        Assert.That(result2024.Items[11].Expenses, Is.EqualTo(-415m));

        Assert.That(result2025.Items[0].BtcTotal, Is.EqualTo(1.000916m));
        Assert.That(result2025.Items[0].FiatTotal, Is.EqualTo(577008.47m));
        Assert.That(result2025.Items[0].BtcMonthlyChange, Is.EqualTo(0.02m));
        Assert.That(result2025.Items[0].BtcYearlyChange, Is.EqualTo(0.02m));
        Assert.That(result2025.Items[0].Income, Is.EqualTo(1383.33m));
        Assert.That(result2025.Items[0].Expenses, Is.EqualTo(-415m));

        Assert.That(result2025.Items[11].BtcTotal, Is.EqualTo(1.001693m));
        Assert.That(result2025.Items[11].FiatTotal, Is.EqualTo(588184.32m));
        Assert.That(result2025.Items[11].BtcMonthlyChange, Is.EqualTo(0.01m));
        Assert.That(result2025.Items[11].BtcYearlyChange, Is.EqualTo(0.09m));
        Assert.That(result2025.Items[11].Income, Is.EqualTo(1383.33m));
        Assert.That(result2025.Items[11].Expenses, Is.EqualTo(-415m));
    }

    /// <summary>
    /// Pins the yearly-change baseline semantics: every displayed month's "Previous Year"
    /// percentage is computed against the position at the END OF THE PREVIOUS CALENDAR YEAR
    /// (December of the prior year), not against the same month one year earlier.
    /// The change percentages are asset-inclusive (Total Wealth basis): each side of the
    /// comparison folds in the latest wealth snapshot on or before that month's end — a
    /// missing snapshot contributes 0 assets to that side (the Assets column's own fallback).
    /// The guard assertions on June 2025 fail if the baseline is ever switched to a
    /// rolling same-month-last-year comparison or back to fiat/btc-only balances.
    /// </summary>
    [Test]
    public async Task YearlyChange_Uses_End_Of_Previous_Calendar_Year_As_Baseline()
    {
        // Dec-2024 baseline snapshot (end of previous calendar year) + a mid-2025 snapshot.
        var baselineSnapshot = (Day: new DateOnly(2024, 12, 15), Fiat: 100000m, Sats: 200000000L);
        var mid2025Snapshot = (Day: new DateOnly(2025, 3, 15), Fiat: 150000m, Sats: 300000000L);
        var snapshots = new[] { baselineSnapshot, mid2025Snapshot };
        SeedSnapshot(baselineSnapshot.Day, baselineSnapshot.Fiat, baselineSnapshot.Sats, FiatCurrency.Brl.Code);
        SeedSnapshot(mid2025Snapshot.Day, mid2025Snapshot.Fiat, mid2025Snapshot.Sats, FiatCurrency.Brl.Code);

        var baseDate = new DateOnly(2025, 12, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>(), _localDatabase);

        var result2024 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2024, 01, 01), new DateOnly(2024, 12, 31)),
            FiatCurrency.Brl, provider);
        var result2025 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 12, 31)),
            FiatCurrency.Brl, provider);

        var dec2024Item = result2024.Items.Single(x => x.MonthYear == new DateOnly(2024, 12, 1));

        Assert.That(result2025.Items, Has.Count.EqualTo(12));

        foreach (var item in result2025.Items)
        {
            var snapshotFiat = SnapshotFiatForMonth(item.MonthYear, snapshots);
            var snapshotBtc = SnapshotSatsForMonth(item.MonthYear, snapshots) / SatoshisPerBitcoin;
            Assert.Multiple(() =>
            {
                Assert.That(item.BtcYearlyChange,
                    Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                        dec2024Item.BtcTotal + baselineSnapshot.Sats / SatoshisPerBitcoin,
                        item.BtcTotal + snapshotBtc)),
                    $"BtcYearlyChange for {item.MonthYear:yyyy-MM} must use Dec-2024 (end of previous calendar year) Total Wealth incl. snapshots as baseline");
                Assert.That(item.FiatYearlyChange,
                    Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                        dec2024Item.FiatTotal + baselineSnapshot.Fiat,
                        item.FiatTotal + snapshotFiat)),
                    $"FiatYearlyChange for {item.MonthYear:yyyy-MM} must use Dec-2024 (end of previous calendar year) Total Wealth incl. snapshots as baseline");
            });
        }

        // Guard: with the daily-accumulating seed, June-2024 and Dec-2024 totals differ, so a
        // rolling same-month-last-year baseline would produce a different percentage for June 2025.
        var june2024Item = result2024.Items.Single(x => x.MonthYear == new DateOnly(2024, 6, 1));
        var june2025Item = result2025.Items.Single(x => x.MonthYear == new DateOnly(2025, 6, 1));
        var june2025SnapshotFiat = SnapshotFiatForMonth(june2025Item.MonthYear, snapshots);
        var june2025SnapshotBtc = SnapshotSatsForMonth(june2025Item.MonthYear, snapshots) / SatoshisPerBitcoin;

        Assert.Multiple(() =>
        {
            Assert.That(june2024Item.BtcTotal, Is.Not.EqualTo(dec2024Item.BtcTotal),
                "Seed sanity: June-2024 and Dec-2024 BTC totals must differ for the guard to discriminate");
            Assert.That(june2025Item.BtcYearlyChange,
                Is.Not.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                    june2024Item.BtcTotal, june2025Item.BtcTotal + june2025SnapshotBtc)),
                "BtcYearlyChange must NOT equal a rolling same-month-last-year (June-2024) comparison");
            Assert.That(june2025Item.FiatYearlyChange,
                Is.Not.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                    june2024Item.FiatTotal, june2025Item.FiatTotal + june2025SnapshotFiat)),
                "FiatYearlyChange must NOT equal a rolling same-month-last-year (June-2024) comparison");
            Assert.That(june2025Item.BtcYearlyChange,
                Is.Not.EqualTo(FinancialCalculator.CalculateImprovementPercentage(dec2024Item.BtcTotal, june2025Item.BtcTotal)),
                "BtcYearlyChange must NOT be the btc-only (snapshot-less) comparison");
            Assert.That(june2025Item.FiatYearlyChange,
                Is.Not.EqualTo(FinancialCalculator.CalculateImprovementPercentage(dec2024Item.FiatTotal, june2025Item.FiatTotal)),
                "FiatYearlyChange must NOT be the fiat-only (snapshot-less) comparison");
        });
    }

    /// <summary>
    /// Snapshots that exist only in the current year must NOT force the yearly change to 0%:
    /// the prior-calendar-year baseline simply contributes 0 assets on its side (the same
    /// fallback as the Assets column), and the percentage is still computed against the
    /// monthly wealth totals on both sides.
    /// </summary>
    [Test]
    public async Task YearlyChange_Falls_Back_To_Zero_Assets_When_Baseline_Snapshot_Missing()
    {
        var snapshot = (Day: new DateOnly(2025, 6, 15), Fiat: 150000m, Sats: 300000000L);
        var snapshots = new[] { snapshot };
        SeedSnapshot(snapshot.Day, snapshot.Fiat, snapshot.Sats, FiatCurrency.Brl.Code);

        var baseDate = new DateOnly(2025, 12, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>(), _localDatabase);

        var result2024 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2024, 01, 01), new DateOnly(2024, 12, 31)),
            FiatCurrency.Brl, provider);
        var result2025 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 12, 31)),
            FiatCurrency.Brl, provider);

        var dec2024Item = result2024.Items.Single(x => x.MonthYear == new DateOnly(2024, 12, 1));

        Assert.That(result2025.Items, Has.Count.EqualTo(12));

        foreach (var item in result2025.Items)
        {
            // Baseline side has no snapshot at all → contributes 0 assets; current side folds its own.
            var snapshotFiat = SnapshotFiatForMonth(item.MonthYear, snapshots);
            var snapshotBtc = SnapshotSatsForMonth(item.MonthYear, snapshots) / SatoshisPerBitcoin;
            Assert.Multiple(() =>
            {
                Assert.That(item.FiatYearlyChange,
                    Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(dec2024Item.FiatTotal, item.FiatTotal + snapshotFiat)),
                    $"FiatYearlyChange for {item.MonthYear:yyyy-MM} must be computed with 0 assets on the baseline side");
                Assert.That(item.BtcYearlyChange,
                    Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(dec2024Item.BtcTotal, item.BtcTotal + snapshotBtc)),
                    $"BtcYearlyChange for {item.MonthYear:yyyy-MM} must be computed with 0 assets on the baseline side");
            });
        }

        // December 2025 has a snapshot on the current side only: the % is computed, not forced to 0.
        var december = result2025.Items.Single(x => x.MonthYear == new DateOnly(2025, 12, 1));
        Assert.Multiple(() =>
        {
            Assert.That(december.AssetsTotalFiat, Is.EqualTo(150000m));
            Assert.That(december.FiatYearlyChange, Is.Not.Zero);
        });
    }

    /// <summary>
    /// Monthly changes fold each month's own latest snapshot into BOTH sides of the
    /// comparison: the previous month's snapshot adjusts the baseline, the current month's
    /// snapshot adjusts the current wealth — matching the asset-inclusive Total Wealth basis
    /// used by the yearly change.
    /// </summary>
    [Test]
    public async Task MonthlyChange_Folds_Snapshots_Into_Both_Sides()
    {
        var octoberSnapshot = (Day: new DateOnly(2024, 10, 10), Fiat: 100000m, Sats: 200000000L);
        var novemberSnapshot = (Day: new DateOnly(2024, 11, 20), Fiat: 150000m, Sats: 300000000L);
        SeedSnapshot(octoberSnapshot.Day, octoberSnapshot.Fiat, octoberSnapshot.Sats, FiatCurrency.Brl.Code);
        SeedSnapshot(novemberSnapshot.Day, novemberSnapshot.Fiat, novemberSnapshot.Sats, FiatCurrency.Brl.Code);

        var result = await BuildBrlReportFor2024();

        var september = result.Items.Single(x => x.MonthYear == new DateOnly(2024, 9, 1));
        var october = result.Items.Single(x => x.MonthYear == new DateOnly(2024, 10, 1));
        var november = result.Items.Single(x => x.MonthYear == new DateOnly(2024, 11, 1));

        Assert.Multiple(() =>
        {
            // October: previous month (September) has no snapshot → 0 on that side; October folds its own.
            Assert.That(october.FiatMonthlyChange,
                Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                    september.FiatTotal, october.FiatTotal + octoberSnapshot.Fiat)));
            Assert.That(october.BtcMonthlyChange,
                Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                    september.BtcTotal, october.BtcTotal + octoberSnapshot.Sats / SatoshisPerBitcoin)));

            // November: both sides fold their own month's latest snapshot.
            Assert.That(november.FiatMonthlyChange,
                Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                    october.FiatTotal + octoberSnapshot.Fiat, november.FiatTotal + novemberSnapshot.Fiat)));
            Assert.That(november.BtcMonthlyChange,
                Is.EqualTo(FinancialCalculator.CalculateImprovementPercentage(
                    october.BtcTotal + octoberSnapshot.Sats / SatoshisPerBitcoin,
                    november.BtcTotal + novemberSnapshot.Sats / SatoshisPerBitcoin)));

            // Discriminator: must not be the fiat/btc-only comparison.
            Assert.That(november.FiatMonthlyChange,
                Is.Not.EqualTo(FinancialCalculator.CalculateImprovementPercentage(october.FiatTotal, november.FiatTotal)));
            Assert.That(november.BtcMonthlyChange,
                Is.Not.EqualTo(FinancialCalculator.CalculateImprovementPercentage(october.BtcTotal, november.BtcTotal)));
        });
    }

    /// <summary>
    /// The seed starts at 2024-01-01, so Dec-2023 never exists: every month of 2024 has no
    /// previous-calendar-year data and must report 0% yearly change (generalizes the Jan/Dec
    /// spot-checks in Should_Calculate_MonthlyTotals).
    /// </summary>
    [Test]
    public async Task YearlyChange_Is_Zero_When_Previous_Calendar_Year_Has_No_Data()
    {
        var result2024 = await BuildBrlReportFor2024();

        Assert.That(result2024.Items, Has.Count.EqualTo(12));

        foreach (var item in result2024.Items)
        {
            Assert.Multiple(() =>
            {
                Assert.That(item.BtcYearlyChange, Is.Zero,
                    $"BtcYearlyChange for {item.MonthYear:yyyy-MM} must be 0% when the previous calendar year has no data");
                Assert.That(item.FiatYearlyChange, Is.Zero,
                    $"FiatYearlyChange for {item.MonthYear:yyyy-MM} must be 0% when the previous calendar year has no data");
            });
        }
    }

    [Test]
    public async Task October_Item_Should_Carry_Latest_Snapshot_On_Or_Before_Month_End()
    {
        SeedSnapshot(new DateOnly(2024, 10, 10), 100000m, 200000000L, FiatCurrency.Brl.Code);

        var result = await BuildBrlReportFor2024();

        var october = result.Items.Single(x => x.MonthYear == new DateOnly(2024, 10, 1));
        Assert.That(october.AssetsTotalFiat, Is.EqualTo(100000m));
        Assert.That(october.AssetsTotalSats, Is.EqualTo(200000000L));
    }

    [Test]
    public async Task Month_Before_Any_Snapshot_Should_Carry_Zero_Asset_Totals()
    {
        SeedSnapshot(new DateOnly(2024, 10, 10), 100000m, 200000000L, FiatCurrency.Brl.Code);

        var result = await BuildBrlReportFor2024();

        var september = result.Items.Single(x => x.MonthYear == new DateOnly(2024, 9, 1));
        Assert.That(september.AssetsTotalFiat, Is.Zero);
        Assert.That(september.AssetsTotalSats, Is.Zero);
    }

    [Test]
    public async Task Currency_Mismatched_Snapshot_Should_Not_Be_Joined()
    {
        SeedSnapshot(new DateOnly(2024, 10, 10), 100000m, 200000000L, FiatCurrency.Brl.Code);

        var baseDate = new DateOnly(2024, 12, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>(), _localDatabase);

        var result = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            FiatCurrency.Usd, provider);

        var october = result.Items.Single(x => x.MonthYear == new DateOnly(2024, 10, 1));
        Assert.That(october.AssetsTotalFiat, Is.Zero);
        Assert.That(october.AssetsTotalSats, Is.Zero);
    }

    [Test]
    public async Task Two_Snapshots_In_One_Month_Should_Use_The_Latest()
    {
        SeedSnapshot(new DateOnly(2024, 10, 10), 100000m, 100L, FiatCurrency.Brl.Code);
        SeedSnapshot(new DateOnly(2024, 10, 20), 150000m, 200L, FiatCurrency.Brl.Code);

        var result = await BuildBrlReportFor2024();

        var october = result.Items.Single(x => x.MonthYear == new DateOnly(2024, 10, 1));
        Assert.That(october.AssetsTotalFiat, Is.EqualTo(150000m));
        Assert.That(october.AssetsTotalSats, Is.EqualTo(200L));
    }

    #region Exclusion Flag Tests

    [Test]
    public async Task Flagged_Income_Is_Absent_From_Income_Bucket_But_Counts_In_Balance()
    {
        var before = await BuildBrlReportFor2024();

        var entity = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2024, 6, 15),
            Name = "Flagged Income",
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 500m, true),
            IsExcluded = true
        }.Build();
        _insertedTransactionIds.Add(entity.Id);
        _localDatabase.GetTransactions().Insert(entity);

        var after = await BuildBrlReportFor2024();
        var mayBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 5, 1));
        var juneBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 6, 1));
        var mayAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 5, 1));
        var juneAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 6, 1));

        Assert.Multiple(() =>
        {
            // flagged income absent from the income bucket
            Assert.That(juneAfter.Income, Is.EqualTo(juneBefore.Income));
            // earlier month untouched
            Assert.That(mayAfter.FiatTotal, Is.EqualTo(mayBefore.FiatTotal));
            // balance still accumulates the flagged transaction: +500 USD * 5.5 BRL/USD
            Assert.That(juneAfter.FiatTotal - mayAfter.FiatTotal,
                Is.EqualTo(juneBefore.FiatTotal - mayBefore.FiatTotal + 2750m));
        });
    }

    [Test]
    public async Task Flagged_Expense_Is_Absent_From_Expense_Bucket_But_Counts_In_Balance()
    {
        var before = await BuildBrlReportFor2024();

        var entity = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2024, 7, 15),
            Name = "Flagged Expense",
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 500m, false),
            IsExcluded = true
        }.Build();
        _insertedTransactionIds.Add(entity.Id);
        _localDatabase.GetTransactions().Insert(entity);

        var after = await BuildBrlReportFor2024();
        var juneBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 6, 1));
        var julyBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 7, 1));
        var juneAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 6, 1));
        var julyAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 7, 1));

        Assert.Multiple(() =>
        {
            // flagged expense absent from the expense bucket
            Assert.That(julyAfter.Expenses, Is.EqualTo(julyBefore.Expenses));
            // earlier month untouched
            Assert.That(juneAfter.FiatTotal, Is.EqualTo(juneBefore.FiatTotal));
            // balance still accumulates the flagged transaction: -500 USD * 5.5 BRL/USD
            Assert.That(julyAfter.FiatTotal - juneAfter.FiatTotal,
                Is.EqualTo(julyBefore.FiatTotal - juneBefore.FiatTotal - 2750m));
        });
    }

    [Test]
    public async Task Unflagged_Transaction_In_Excluded_Category_Remains_Excluded()
    {
        CategoryId excludedCategoryId = IdGenerator.Generate();
        var excludedCategory = new CategoryBuilder()
            .WithId(excludedCategoryId)
            .WithName("Excluded Category")
            .WithIcon(Icon.Empty)
            .Build();
        _localDatabase.GetCategories().Insert(excludedCategory);

        var excludedCategoryIds = new HashSet<string> { excludedCategoryId.Value };
        var before = await BuildBrlReportFor2024(excludedCategoryIds);

        var entity = new TransactionBuilder()
        {
            CategoryId = excludedCategoryId,
            Date = new DateOnly(2024, 8, 15),
            Name = "Unflagged Income In Excluded Category",
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 500m, true)
        }.Build();
        _insertedTransactionIds.Add(entity.Id);
        _localDatabase.GetTransactions().Insert(entity);

        var after = await BuildBrlReportFor2024(excludedCategoryIds);
        var julyBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 7, 1));
        var augustBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 8, 1));
        var julyAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 7, 1));
        var augustAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 8, 1));

        Assert.Multiple(() =>
        {
            // category exclusion still applies independently of the transaction flag
            Assert.That(augustAfter.Income, Is.EqualTo(augustBefore.Income));
            // earlier month untouched
            Assert.That(julyAfter.FiatTotal, Is.EqualTo(julyBefore.FiatTotal));
            // balance still accumulates the transaction
            Assert.That(augustAfter.FiatTotal - julyAfter.FiatTotal,
                Is.EqualTo(augustBefore.FiatTotal - julyBefore.FiatTotal + 2750m));
        });
    }

    [Test]
    public async Task Flagged_Transaction_Does_Not_Affect_Purchase_And_Sale_Buckets()
    {
        var before = await BuildBrlReportFor2024();

        var purchaseEntity = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2024, 9, 15),
            Name = "Flagged Bitcoin Purchase",
            TransactionDetails = new FiatToBitcoinDetails(_usdAccount.Id.ToString(), _btcAccount.Id.ToString(),
                FiatValue.New(1000m), BtcValue.ParseSats(100000)),
            IsExcluded = true
        }.Build();
        _insertedTransactionIds.Add(purchaseEntity.Id);
        _localDatabase.GetTransactions().Insert(purchaseEntity);

        var saleEntity = new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2024, 10, 15),
            Name = "Flagged Bitcoin Sale",
            TransactionDetails = new BitcoinToFiatDetails(_btcAccount.Id.ToString(), _usdAccount.Id.ToString(),
                BtcValue.ParseSats(10000), FiatValue.New(1000m)),
            IsExcluded = true
        }.Build();
        _insertedTransactionIds.Add(saleEntity.Id);
        _localDatabase.GetTransactions().Insert(saleEntity);

        var after = await BuildBrlReportFor2024();
        var augustBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 8, 1));
        var septemberBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 9, 1));
        var octoberBefore = before.Items.Single(x => x.MonthYear == new DateOnly(2024, 10, 1));
        var augustAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 8, 1));
        var septemberAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 9, 1));
        var octoberAfter = after.Items.Single(x => x.MonthYear == new DateOnly(2024, 10, 1));

        Assert.Multiple(() =>
        {
            // purchase bucket unaffected by the flag
            Assert.That(septemberAfter.BitcoinPurchased, Is.EqualTo(septemberBefore.BitcoinPurchased + 0.001m));
            // sale bucket unaffected by the flag
            Assert.That(octoberAfter.BitcoinSold, Is.EqualTo(octoberBefore.BitcoinSold - 0.0001m));
            // earlier month untouched
            Assert.That(augustAfter.FiatTotal, Is.EqualTo(augustBefore.FiatTotal));
            // balances still move for both flagged transactions (BTC leg valued at the fake price:
            // +0.001 BTC * 100k USD/BTC * 5.5 BRL/USD = +550 on purchase month, mirrored on sale)
            Assert.That(septemberAfter.FiatTotal - augustAfter.FiatTotal,
                Is.EqualTo(septemberBefore.FiatTotal - augustBefore.FiatTotal - 4950m));
            Assert.That(octoberAfter.FiatTotal - septemberAfter.FiatTotal,
                Is.EqualTo(octoberBefore.FiatTotal - septemberBefore.FiatTotal + 5445m));
        });
    }

    #endregion

    private async Task<MonthlyTotalsData> BuildBrlReportFor2024(IReadOnlySet<string>? excludedCategoryIds = null)
    {
        var baseDate = new DateOnly(2024, 12, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>(), _localDatabase);

        return await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            FiatCurrency.Brl, provider, excludedCategoryIds);
    }

    private const decimal SatoshisPerBitcoin = 100_000_000m;

    private static decimal SnapshotFiatForMonth(DateOnly monthYear, IReadOnlyList<(DateOnly Day, decimal Fiat, long Sats)> snapshots)
    {
        var monthEnd = new DateOnly(monthYear.Year, monthYear.Month, DateTime.DaysInMonth(monthYear.Year, monthYear.Month));
        return snapshots.Where(s => s.Day <= monthEnd).OrderBy(s => s.Day).LastOrDefault().Fiat;
    }

    private static long SnapshotSatsForMonth(DateOnly monthYear, IReadOnlyList<(DateOnly Day, decimal Fiat, long Sats)> snapshots)
    {
        var monthEnd = new DateOnly(monthYear.Year, monthYear.Month, DateTime.DaysInMonth(monthYear.Year, monthYear.Month));
        return snapshots.Where(s => s.Day <= monthEnd).OrderBy(s => s.Day).LastOrDefault().Sats;
    }

    private void SeedSnapshot(DateOnly day, decimal totalFiat, long totalSats, string currencyCode)
    {
        _localDatabase.GetWealthSnapshots().Insert(new WealthSnapshotEntity
        {
            Id = ObjectId.NewObjectId(),
            SnapshotDateTimeUtc = day.ToDateTime(new TimeOnly(12, 0, 0), DateTimeKind.Utc),
            SnapshotDay = day,
            TotalValueInMainCurrency = totalFiat,
            TotalValueInSats = totalSats,
            MainCurrencyCode = currencyCode
        });
    }
}
