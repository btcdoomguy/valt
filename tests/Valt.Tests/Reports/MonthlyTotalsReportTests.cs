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
