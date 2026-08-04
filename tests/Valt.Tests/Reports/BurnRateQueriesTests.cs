using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Modules.SpendingAnalytics.Queries;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class BurnRateQueriesTests : DatabaseTest
{
    private AccountEntity _brlAccount = null!;
    private CategoryId _categoryId = null!;

    protected override Task SeedDatabase()
    {
        _categoryId = IdGenerator.Generate();

        _brlAccount = new FiatAccountBuilder()
        {
            Name = "BRL Account",
            FiatCurrency = FiatCurrency.Brl,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_brlAccount);

        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity() { Date = currentDate, Price = 100000m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity() { Date = currentDate, Currency = FiatCurrency.Brl.Code, Price = 5.5m });
            currentDate = currentDate.AddDays(1);
        }

        return base.SeedDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        _localDatabase.GetTransactions().DeleteAll();
    }

    [Test]
    public async Task Should_Calculate_Projection_When_Day_Greater_Than_Five()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 15));
        AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
        AddExpenseTransaction(new DateOnly(2025, 2, 15), 1000m);
        AddExpenseTransaction(new DateOnly(2025, 3, 10), 750m);

        var result = await ExecuteQuery(clock, 10000m);

        var avgDaily = 750m / 15m;
        var projected = avgDaily * DateTime.DaysInMonth(2025, 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasData, Is.True);
            Assert.That(result.DayOfMonth, Is.EqualTo(15));
            Assert.That(result.SpentSoFar, Is.EqualTo(750m));
            Assert.That(result.AvgDailySpend, Is.EqualTo(avgDaily));
            Assert.That(result.ProjectedMonthEnd, Is.EqualTo(projected));
            Assert.That(result.MedianMonthlyExpenses, Is.EqualTo(1000m));
            Assert.That(result.VsMedianPercent, Is.EqualTo(Math.Round((projected - 1000m) / 1000m * 100, 2)));
        }
    }

    [Test]
    public async Task Should_Not_Project_When_Day_Less_Than_Five()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 3));
        AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
        AddExpenseTransaction(new DateOnly(2025, 2, 15), 1000m);
        AddExpenseTransaction(new DateOnly(2025, 3, 2), 300m);

        var result = await ExecuteQuery(clock, 10000m);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasData, Is.True);
            Assert.That(result.DayOfMonth, Is.EqualTo(3));
            Assert.That(result.SpentSoFar, Is.EqualTo(300m));
            Assert.That(result.AvgDailySpend, Is.EqualTo(100m));
            Assert.That(result.ProjectedMonthEnd, Is.Null);
            Assert.That(result.VsMedianPercent, Is.Null);
        }
    }

    [Test]
    public async Task Should_Reuse_StatisticsReport_Median()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 15));
        AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
        AddExpenseTransaction(new DateOnly(2025, 2, 15), 2000m);
        AddExpenseTransaction(new DateOnly(2025, 3, 10), 750m);

        var result = await ExecuteQuery(clock, 10000m);

        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
        var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);
        var stats = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

        Assert.That(result.MedianMonthlyExpenses, Is.EqualTo(stats.MedianMonthlyExpenses.Value));
    }

    [Test]
    public async Task Should_Return_VsMedian_Null_When_Median_Is_Zero()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 15));
        AddExpenseTransaction(new DateOnly(2025, 3, 10), 750m);

        var result = await ExecuteQuery(clock, 10000m);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasData, Is.True);
            Assert.That(result.MedianMonthlyExpenses, Is.EqualTo(0m));
            Assert.That(result.VsMedianPercent, Is.Null);
        }
    }

    [Test]
    public async Task Should_Return_HasData_False_When_No_MTD_Transactions()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 15));
        AddExpenseTransaction(new DateOnly(2025, 2, 15), 1000m);

        var result = await ExecuteQuery(clock, 10000m);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasData, Is.False);
            Assert.That(result.SpentSoFar, Is.EqualTo(0m));
            Assert.That(result.AvgDailySpend, Is.EqualTo(0m));
        }
    }

    private async Task<BurnRateDataDto> ExecuteQuery(FakeClock clock, decimal currentWealth)
    {
        var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
        var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);
        var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        };
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);
        var sut = new BurnRateQueries(factory, monthlyTotalsReport, statisticsReport, clock, currencySettings);

        return await sut.GetBurnRateAsync(new GetBurnRateQuery
        {
            CurrentWealthInFiat = currentWealth,
            AccountIds = [_brlAccount.Id.ToString()]
        });
    }

    private void AddExpenseTransaction(DateOnly date, decimal amount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = date,
            Name = $"Expense {date}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, false)
        }.Build());
    }
}
