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
public class SavingsRateQueriesTests : DatabaseTest
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
    public async Task Should_Calculate_Positive_SavingsRate_When_Income_Exceeds_Expenses()
    {
        var month = new DateOnly(2025, 1, 1);
        AddIncomeTransaction(month, 1000m);
        AddExpenseTransaction(month, 750m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthRate = GetMonthRate(result, month);

        Assert.That(monthRate, Is.EqualTo(25m));
    }

    [Test]
    public async Task Should_Return_Null_Rate_When_Income_Is_Zero()
    {
        var month = new DateOnly(2025, 1, 1);
        AddExpenseTransaction(month, 500m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthRate = GetMonthRate(result, month);

        Assert.That(monthRate, Is.Null);
    }

    [Test]
    public async Task Should_Return_Negative_SavingsRate_When_Expenses_Exceed_Income()
    {
        var month = new DateOnly(2025, 1, 1);
        AddIncomeTransaction(month, 1000m);
        AddExpenseTransaction(month, 1250m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthRate = GetMonthRate(result, month);

        Assert.That(monthRate, Is.EqualTo(-25m));
    }

    [Test]
    public async Task Should_Exclude_Current_Incomplete_Month()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 15));
        AddIncomeTransaction(new DateOnly(2025, 2, 1), 1000m);
        AddExpenseTransaction(new DateOnly(2025, 2, 1), 750m);
        AddIncomeTransaction(new DateOnly(2025, 3, 1), 5000m);
        AddExpenseTransaction(new DateOnly(2025, 3, 1), 1000m);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), clock);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetMonthRate(result, new DateOnly(2025, 2, 1)), Is.EqualTo(25m));
            Assert.That(GetMonthRate(result, new DateOnly(2025, 3, 1)), Is.Null);
        }
    }

    [Test]
    public async Task Should_Return_Empty_Months_When_No_Transactions()
    {
        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Months, Is.Empty);
            Assert.That(result.PrimaryCurrency, Is.EqualTo(FiatCurrency.Brl.Code));
        }
    }

    private static decimal? GetMonthRate(SavingsRateDataDto result, DateOnly month)
    {
        return result.Months.FirstOrDefault(x => x.Month == month)?.Rate;
    }

    [Test]
    public async Task Should_Change_Rate_When_Category_Filter_Excludes_One_Category()
    {
        var categoryA = IdGenerator.Generate();
        var categoryB = IdGenerator.Generate();
        InsertCategory(categoryA);
        InsertCategory(categoryB);

        var month = new DateOnly(2025, 1, 1);
        AddIncomeTransaction(month, 1000m, categoryA);
        AddExpenseTransaction(month, 500m, categoryB);

        var clock = new FakeClock(new DateTime(2025, 12, 31));

        var withBoth = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), clock,
            [categoryA.ToString(), categoryB.ToString()]);
        var withOnlyA = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), clock,
            [categoryA.ToString()]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetMonthRate(withBoth, month), Is.EqualTo(50m));
            Assert.That(GetMonthRate(withOnlyA, month), Is.EqualTo(100m));
        }
    }

    private async Task<SavingsRateDataDto> ExecuteQuery(DateOnly from, DateOnly to, FakeClock clock, string[] categoryIds)
    {
        var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
        var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);
        var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        };
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);
        var sut = new SavingsRateQueries(factory, monthlyTotalsReport, clock, currencySettings);

        return await sut.GetSavingsRateAsync(new GetSavingsRateQuery
        {
            From = from,
            To = to,
            AccountIds = [_brlAccount.Id.ToString()],
            CategoryIds = categoryIds
        });
    }

    private async Task<SavingsRateDataDto> ExecuteQuery(DateOnly from, DateOnly to, FakeClock clock)
    {
        return await ExecuteQuery(from, to, clock, []);
    }

    private async Task<SavingsRateDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), []);
    }

    private async Task<SavingsRateDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate, string[] categoryIds)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), categoryIds);
    }

    private void InsertCategory(CategoryId id)
    {
        _localDatabase.GetCategories().Insert(CategoryBuilder.ACategory()
            .WithId(id)
            .WithName($"Category {id}")
            .Build());
    }

    private void AddIncomeTransaction(DateOnly month, decimal amount, CategoryId categoryId)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = categoryId,
            Date = new DateOnly(month.Year, month.Month, 10),
            Name = $"Income {month}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, true)
        }.Build());
    }

    private void AddIncomeTransaction(DateOnly month, decimal amount)
    {
        AddIncomeTransaction(month, amount, _categoryId);
    }

    private void AddExpenseTransaction(DateOnly month, decimal amount, CategoryId categoryId)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = categoryId,
            Date = new DateOnly(month.Year, month.Month, 15),
            Name = $"Expense {month}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, false)
        }.Build());
    }

    private void AddExpenseTransaction(DateOnly month, decimal amount)
    {
        AddExpenseTransaction(month, amount, _categoryId);
    }
}
