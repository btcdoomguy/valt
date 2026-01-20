using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.WealthOverview;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class WealthOverviewReportTests : DatabaseTest
{
    private AccountEntity _btcAccount = null!;
    private AccountEntity _usdAccount = null!;

    protected override Task SeedDatabase()
    {
        // Initialize demo accounts
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
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        // Seed rates for the test period
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity
            {
                Date = currentDate,
                Price = 50000m
            });
            currentDate = currentDate.AddDays(1);
        }

        // Create a transaction on January 15, 2025 to establish the min transaction date
        _localDatabase.GetTransactions().Insert(new TransactionBuilder
        {
            Id = IdGenerator.Generate(),
            Date = new DateOnly(2025, 1, 15),
            Name = "Initial USD Transaction",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 100m, true)
        }.Build());

        return base.SeedDatabase();
    }

    [Test]
    public async Task Should_Return_Daily_Data_For_Last_12_Days()
    {
        var clock = new FakeClock(new DateTime(2025, 1, 25));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new WealthOverviewReport(clock, new NullLogger<WealthOverviewReport>());

        var result = await report.GetAsync(WealthOverviewPeriod.Daily, FiatCurrency.Usd, provider);

        Assert.That(result.MainCurrency, Is.EqualTo(FiatCurrency.Usd));
        Assert.That(result.Period, Is.EqualTo(WealthOverviewPeriod.Daily));
        Assert.That(result.Items.Count, Is.LessThanOrEqualTo(12));
        Assert.That(result.Items.Count, Is.GreaterThan(0));

        // Labels should be in "MMM dd" format
        Assert.That(result.Items[0].Label, Does.Match(@"[A-Z][a-z]{2} \d{2}"));

        // Verify items are ordered chronologically
        for (var i = 1; i < result.Items.Count; i++)
        {
            Assert.That(result.Items[i].PeriodEnd, Is.GreaterThan(result.Items[i - 1].PeriodEnd));
        }
    }

    [Test]
    public async Task Should_Return_Weekly_Data_With_Saturday_As_Period_End()
    {
        // Wednesday January 15, 2025
        var clock = new FakeClock(new DateTime(2025, 1, 15));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new WealthOverviewReport(clock, new NullLogger<WealthOverviewReport>());

        var result = await report.GetAsync(WealthOverviewPeriod.Weekly, FiatCurrency.Usd, provider);

        Assert.That(result.Period, Is.EqualTo(WealthOverviewPeriod.Weekly));
        Assert.That(result.Items.Count, Is.LessThanOrEqualTo(12));

        // The most recent item should be this Saturday (Jan 18, 2025)
        var lastItem = result.Items.Last();
        Assert.That(lastItem.PeriodEnd.DayOfWeek, Is.EqualTo(DayOfWeek.Saturday));
        Assert.That(lastItem.PeriodEnd, Is.EqualTo(new DateOnly(2025, 1, 18)));

        // All period ends should be Saturdays
        foreach (var item in result.Items)
        {
            Assert.That(item.PeriodEnd.DayOfWeek, Is.EqualTo(DayOfWeek.Saturday));
        }
    }

    [Test]
    public async Task Should_Handle_Sparse_Data_With_Only_Few_Periods()
    {
        // Clock set to January 17, 2025 - only 2 days after first transaction (Jan 15)
        var clock = new FakeClock(new DateTime(2025, 1, 17));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new WealthOverviewReport(clock, new NullLogger<WealthOverviewReport>());

        var result = await report.GetAsync(WealthOverviewPeriod.Daily, FiatCurrency.Usd, provider);

        // Should only return items for dates >= min transaction date (Jan 15)
        Assert.That(result.Items.All(x => x.PeriodEnd >= new DateOnly(2025, 1, 15)), Is.True);
    }

    [Test]
    public async Task Should_Return_Monthly_Data_For_Last_12_Months()
    {
        var clock = new FakeClock(new DateTime(2025, 1, 25));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new WealthOverviewReport(clock, new NullLogger<WealthOverviewReport>());

        var result = await report.GetAsync(WealthOverviewPeriod.Monthly, FiatCurrency.Usd, provider);

        Assert.That(result.Period, Is.EqualTo(WealthOverviewPeriod.Monthly));
        Assert.That(result.Items.Count, Is.LessThanOrEqualTo(12));

        // Labels should be in "MMM yyyy" format
        var lastItem = result.Items.Last();
        Assert.That(lastItem.Label, Does.Match(@"[A-Z][a-z]{2} \d{4}"));

        // Last item should be end of current month (Jan 31, 2025)
        Assert.That(lastItem.PeriodEnd, Is.EqualTo(new DateOnly(2025, 1, 31)));
    }

    [Test]
    public async Task Should_Return_Yearly_Data_For_Last_12_Years()
    {
        var clock = new FakeClock(new DateTime(2025, 1, 25));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new WealthOverviewReport(clock, new NullLogger<WealthOverviewReport>());

        var result = await report.GetAsync(WealthOverviewPeriod.Yearly, FiatCurrency.Usd, provider);

        Assert.That(result.Period, Is.EqualTo(WealthOverviewPeriod.Yearly));
        Assert.That(result.Items.Count, Is.LessThanOrEqualTo(12));

        // Labels should be year only
        var lastItem = result.Items.Last();
        Assert.That(lastItem.Label, Is.EqualTo("2025"));

        // Last item should be end of current year (Dec 31, 2025)
        Assert.That(lastItem.PeriodEnd, Is.EqualTo(new DateOnly(2025, 12, 31)));
    }

    [Test]
    public async Task Should_Calculate_Correct_Wealth_At_Period_End()
    {
        var clock = new FakeClock(new DateTime(2025, 1, 25));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new WealthOverviewReport(clock, new NullLogger<WealthOverviewReport>());

        var result = await report.GetAsync(WealthOverviewPeriod.Daily, FiatCurrency.Usd, provider);

        // Get the last item (today)
        var lastItem = result.Items.Last();

        // Initial BTC: 1 BTC = 50000 USD
        // Initial USD: 10000 + 100 (from transaction) = 10100 USD
        // Total: 50000 + 10100 = 60100 USD
        Assert.That(lastItem.FiatTotal, Is.EqualTo(60100m));
        Assert.That(lastItem.BtcTotal, Is.EqualTo(1m));
    }

    [Test]
    public async Task Should_Return_Empty_Items_When_No_Transactions()
    {
        // Create a fresh database with no transactions
        using var emptyDbStream = new MemoryStream();
        var emptyDb = new Infra.DataAccess.LocalDatabase(new Infra.Kernel.Time.Clock());
        emptyDb.OpenInMemoryDatabase(emptyDbStream);

        using var emptyPriceDbStream = new MemoryStream();
        var emptyPriceDb = new Infra.DataAccess.PriceDatabase(
            new Infra.Kernel.Time.Clock(),
            NSubstitute.Substitute.For<Infra.Kernel.Notifications.INotificationPublisher>());
        emptyPriceDb.OpenInMemoryDatabase(emptyPriceDbStream);

        var clock = new FakeClock(new DateTime(2025, 1, 25));
        var provider = new ReportDataProvider(emptyPriceDb, emptyDb, clock);
        var report = new WealthOverviewReport(clock, new NullLogger<WealthOverviewReport>());

        var result = await report.GetAsync(WealthOverviewPeriod.Daily, FiatCurrency.Usd, provider);

        Assert.That(result.Items, Is.Empty);

        emptyDb.CloseDatabase();
        emptyPriceDb.CloseDatabase();
    }
}
