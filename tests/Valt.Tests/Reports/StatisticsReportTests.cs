using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class StatisticsReportTests : DatabaseTest
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

        // Feed fake rates for 2024 and 2025
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

            currentDate = currentDate.AddDays(1);
        }

        return base.SeedDatabase();
    }

    [Test]
    public async Task Should_Return_Zero_When_No_Transactions()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
        var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

        var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.MedianMonthlyExpenses.Value, Is.EqualTo(0m));
            Assert.That(result.WealthCoverageMonths, Is.EqualTo(0));
            Assert.That(result.WealthCoverageFormatted, Is.EqualTo("0"));
        }
    }

    [Test]
    public async Task Should_Return_Zero_Coverage_When_Wealth_Is_Zero()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add some expenses
            AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 2, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 3, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 0m, provider);

            Assert.That(result.WealthCoverageMonths, Is.EqualTo(0));
            Assert.That(result.WealthCoverageFormatted, Is.EqualTo("0"));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_Median_With_Odd_Number_Of_Months()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses for 3 months with different values: 500, 1000, 2000
            // Median should be 1000
            AddExpenseTransaction(new DateOnly(2025, 9, 15), 500m);
            AddExpenseTransaction(new DateOnly(2025, 10, 15), 2000m);
            AddExpenseTransaction(new DateOnly(2025, 11, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            Assert.That(result.MedianMonthlyExpenses.Value, Is.EqualTo(1000m));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_Median_With_Even_Number_Of_Months()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses for 4 months with different values: 500, 1000, 1500, 2000
            // Sorted: 500, 1000, 1500, 2000
            // Median should be average of middle two: (1000 + 1500) / 2 = 1250
            AddExpenseTransaction(new DateOnly(2025, 8, 15), 500m);
            AddExpenseTransaction(new DateOnly(2025, 9, 15), 2000m);
            AddExpenseTransaction(new DateOnly(2025, 10, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 11, 15), 1500m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            Assert.That(result.MedianMonthlyExpenses.Value, Is.EqualTo(1250m));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Format_Wealth_Coverage_In_Months_Only()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expense of 1000/month, wealth of 5000 should give 5 months coverage
            AddExpenseTransaction(new DateOnly(2025, 10, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 11, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 5000m, provider);

            Assert.That(result.WealthCoverageMonths, Is.EqualTo(5));
            Assert.That(result.WealthCoverageFormatted, Is.EqualTo("5"));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Format_Wealth_Coverage_In_Years_And_Months()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expense of 1000/month, wealth of 15000 should give 15 months = 1y 3m
            AddExpenseTransaction(new DateOnly(2025, 10, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 11, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 15000m, provider);

            Assert.That(result.WealthCoverageMonths, Is.EqualTo(15));
            Assert.That(result.WealthCoverageFormatted, Is.EqualTo("1y 3m"));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Format_Wealth_Coverage_In_Years_Only()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expense of 1000/month, wealth of 24000 should give 24 months = 2y
            AddExpenseTransaction(new DateOnly(2025, 10, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 11, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 24000m, provider);

            Assert.That(result.WealthCoverageMonths, Is.EqualTo(24));
            Assert.That(result.WealthCoverageFormatted, Is.EqualTo("2y"));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Exclude_Current_Month_From_Calculation()
    {
        // Test date is Dec 31, 2025 - so December expenses should NOT be included
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expense in December (current month - should be excluded)
            AddExpenseTransaction(new DateOnly(2025, 12, 15), 99999m);
            // Add expense in November (should be included)
            AddExpenseTransaction(new DateOnly(2025, 11, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Should only consider November's 1000, not December's 99999
            Assert.That(result.MedianMonthlyExpenses.Value, Is.EqualTo(1000m));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
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
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, false) // false = expense
        }.Build());
    }
}
