using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
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
using Valt.Infra.Modules.BtcDenominatedMetrics.Queries;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class BtcDenominatedMetricsQueriesTests : DatabaseTest
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
    public async Task Should_Calculate_SatsEarned_SatsSpent_And_StackVelocity_For_Brl_Income_And_Expense()
    {
        var month = new DateOnly(2025, 1, 1);
        AddIncomeTransaction(month, 1000m);
        AddExpenseTransaction(month, 500m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonthData(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.SatsEarned, Is.GreaterThan(0));
            Assert.That(monthData.SatsSpent, Is.LessThan(0));
            Assert.That(monthData.StackVelocity, Is.EqualTo(monthData.SatsEarned - monthData.SatsSpent));
        }
    }

    private static BtcDenominatedMetricsMonthDto? GetMonthData(BtcDenominatedMetricsDataDto result, DateOnly month)
    {
        return result.Months.FirstOrDefault(x => x.Month == month);
    }

    private async Task<BtcDenominatedMetricsDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), []);
    }

    private async Task<BtcDenominatedMetricsDataDto> ExecuteQuery(DateOnly from, DateOnly to, FakeClock clock, string[] categoryIds)
    {
        var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
        var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        };
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);
        var sut = new BtcDenominatedMetricsQueries(factory, monthlyTotalsReport, clock, currencySettings);

        return await sut.GetBtcDenominatedMetricsAsync(new GetBtcDenominatedMetricsQuery
        {
            From = from,
            To = to,
            AccountIds = [_brlAccount.Id.ToString()],
            CategoryIds = categoryIds
        });
    }

    private void AddIncomeTransaction(DateOnly month, decimal amount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = new DateOnly(month.Year, month.Month, 10),
            Name = $"Income {month}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, true)
        }.Build());
    }

    private void AddExpenseTransaction(DateOnly month, decimal amount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = new DateOnly(month.Year, month.Month, 15),
            Name = $"Expense {month}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, false)
        }.Build());
    }
}
