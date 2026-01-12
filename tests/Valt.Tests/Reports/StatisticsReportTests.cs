using Microsoft.Extensions.Logging.Abstractions;
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
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class StatisticsReportTests : DatabaseTest
{
    private AccountEntity _brlAccount = null!;
    private AccountEntity _btcAccount = null!;
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

        _btcAccount = new BtcAccountBuilder()
        {
            Name = "BTC Account",
            Value = BtcValue.New(100000000) // 1 BTC
        }.Build();
        _localDatabase.GetAccounts().Insert(_btcAccount);

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

    [Test]
    public async Task Should_Return_Previous_Period_Median_When_Data_Exists()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses for current period (Jan 2025 - Nov 2025)
            AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 2, 15), 1200m);
            AddExpenseTransaction(new DateOnly(2025, 3, 15), 1100m);

            // Add expenses for previous period (Jan 2024 - Nov 2024)
            AddExpenseTransaction(new DateOnly(2024, 1, 15), 800m);
            AddExpenseTransaction(new DateOnly(2024, 2, 15), 900m);
            AddExpenseTransaction(new DateOnly(2024, 3, 15), 850m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesPreviousPeriod, Is.True);
                Assert.That(result.MedianMonthlyExpensesPreviousPeriod, Is.Not.Null);
                Assert.That(result.MedianMonthlyExpensesPreviousPeriod!.Value, Is.EqualTo(850m));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_Evolution_Percentage_When_Previous_Period_Exists()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses for current period - median should be 1000
            AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 2, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 3, 15), 1000m);

            // Add expenses for previous period - median should be 800
            AddExpenseTransaction(new DateOnly(2024, 1, 15), 800m);
            AddExpenseTransaction(new DateOnly(2024, 2, 15), 800m);
            AddExpenseTransaction(new DateOnly(2024, 3, 15), 800m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Evolution: ((1000 - 800) / 800) * 100 = 25%
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.MedianMonthlyExpensesEvolution, Is.Not.Null);
                Assert.That(result.MedianMonthlyExpensesEvolution!.Value, Is.EqualTo(25m));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_Negative_Evolution_When_Expenses_Decreased()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses for current period - median should be 800
            AddExpenseTransaction(new DateOnly(2025, 1, 15), 800m);
            AddExpenseTransaction(new DateOnly(2025, 2, 15), 800m);
            AddExpenseTransaction(new DateOnly(2025, 3, 15), 800m);

            // Add expenses for previous period - median should be 1000
            AddExpenseTransaction(new DateOnly(2024, 1, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2024, 2, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2024, 3, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Evolution: ((800 - 1000) / 1000) * 100 = -20%
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.MedianMonthlyExpensesEvolution, Is.Not.Null);
                Assert.That(result.MedianMonthlyExpensesEvolution!.Value, Is.EqualTo(-20m));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Not_Have_Previous_Period_When_No_Data_Exists()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses only for current period (no previous period data)
            AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 2, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesPreviousPeriod, Is.False);
                Assert.That(result.MedianMonthlyExpensesPreviousPeriod, Is.Null);
                Assert.That(result.MedianMonthlyExpensesEvolution, Is.Null);
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Not_Calculate_Evolution_When_Current_Period_Has_No_Data()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses only for previous period (no current period data)
            AddExpenseTransaction(new DateOnly(2024, 1, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2024, 2, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Previous period has data, but current doesn't, so evolution cannot be calculated
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesPreviousPeriod, Is.True);
                Assert.That(result.MedianMonthlyExpensesPreviousPeriod, Is.Not.Null);
                Assert.That(result.MedianMonthlyExpensesEvolution, Is.Null);
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    #region Sat-based Median Tests

    [Test]
    public async Task Should_Calculate_Sat_Median_When_Transactions_Have_SatAmount()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses with SatAmount for current period
            // Monthly expenses: Jan=10000, Feb=15000, Mar=12000 -> median=12000
            AddExpenseTransactionWithSats(new DateOnly(2025, 1, 15), 1000m, -10000);
            AddExpenseTransactionWithSats(new DateOnly(2025, 2, 15), 1500m, -15000);
            AddExpenseTransactionWithSats(new DateOnly(2025, 3, 15), 1200m, -12000);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.True);
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(12000));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_Sat_Evolution_When_Both_Periods_Have_Data()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses with SatAmount for current period - median=10000
            AddExpenseTransactionWithSats(new DateOnly(2025, 1, 15), 1000m, -10000);
            AddExpenseTransactionWithSats(new DateOnly(2025, 2, 15), 1000m, -10000);
            AddExpenseTransactionWithSats(new DateOnly(2025, 3, 15), 1000m, -10000);

            // Add expenses with SatAmount for previous period - median=8000
            AddExpenseTransactionWithSats(new DateOnly(2024, 1, 15), 800m, -8000);
            AddExpenseTransactionWithSats(new DateOnly(2024, 2, 15), 800m, -8000);
            AddExpenseTransactionWithSats(new DateOnly(2024, 3, 15), 800m, -8000);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Evolution: ((10000 - 8000) / 8000) * 100 = 25%
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.True);
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(10000));
                Assert.That(result.MedianMonthlyExpensesPreviousPeriodSats, Is.EqualTo(8000));
                Assert.That(result.MedianMonthlyExpensesSatsEvolution, Is.EqualTo(25m));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Not_Have_Sat_Median_When_No_SatAmount_Data()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses WITHOUT SatAmount (using regular expense method)
            AddExpenseTransaction(new DateOnly(2025, 1, 15), 1000m);
            AddExpenseTransaction(new DateOnly(2025, 2, 15), 1000m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.False);
                Assert.That(result.MedianMonthlyExpensesSats, Is.Null);
                Assert.That(result.MedianMonthlyExpensesPreviousPeriodSats, Is.Null);
                Assert.That(result.MedianMonthlyExpensesSatsEvolution, Is.Null);
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_Negative_Sat_Evolution_When_Expenses_Decreased()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add expenses with SatAmount for current period - median=8000
            AddExpenseTransactionWithSats(new DateOnly(2025, 1, 15), 800m, -8000);
            AddExpenseTransactionWithSats(new DateOnly(2025, 2, 15), 800m, -8000);
            AddExpenseTransactionWithSats(new DateOnly(2025, 3, 15), 800m, -8000);

            // Add expenses with SatAmount for previous period - median=10000
            AddExpenseTransactionWithSats(new DateOnly(2024, 1, 15), 1000m, -10000);
            AddExpenseTransactionWithSats(new DateOnly(2024, 2, 15), 1000m, -10000);
            AddExpenseTransactionWithSats(new DateOnly(2024, 3, 15), 1000m, -10000);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Evolution: ((8000 - 10000) / 10000) * 100 = -20%
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(8000));
                Assert.That(result.MedianMonthlyExpensesPreviousPeriodSats, Is.EqualTo(10000));
                Assert.That(result.MedianMonthlyExpensesSatsEvolution, Is.EqualTo(-20m));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Handle_Mixed_Transactions_With_And_Without_SatAmount()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add some expenses with SatAmount
            AddExpenseTransactionWithSats(new DateOnly(2025, 1, 15), 1000m, -10000);
            AddExpenseTransactionWithSats(new DateOnly(2025, 2, 15), 1200m, -12000);

            // Add some expenses without SatAmount (should be ignored for sat median)
            AddExpenseTransaction(new DateOnly(2025, 3, 15), 1500m);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            using (Assert.EnterMultipleScope())
            {
                // Fiat median should include all transactions
                Assert.That(result.MedianMonthlyExpenses.Value, Is.EqualTo(1200m));

                // Sat median should only include transactions with SatAmount
                // Only Jan (10000) and Feb (12000), median = (10000 + 12000) / 2 = 11000
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.True);
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(11000));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_Sat_Median_From_Bitcoin_Transactions()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add BTC expense transactions (direct bitcoin spending)
            // Monthly expenses: Jan=50000, Feb=70000, Mar=60000 -> median=60000
            AddBtcExpenseTransaction(new DateOnly(2025, 1, 15), -50000);
            AddBtcExpenseTransaction(new DateOnly(2025, 2, 15), -70000);
            AddBtcExpenseTransaction(new DateOnly(2025, 3, 15), -60000);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.True);
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(60000));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Combine_Fiat_And_Bitcoin_Transactions_For_Sat_Median()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add fiat expenses with SatAmount - Jan: 10000 sats
            AddExpenseTransactionWithSats(new DateOnly(2025, 1, 15), 1000m, -10000);

            // Add BTC expenses - Feb: 20000 sats
            AddBtcExpenseTransaction(new DateOnly(2025, 2, 15), -20000);

            // Add another fiat expense - Mar: 15000 sats
            AddExpenseTransactionWithSats(new DateOnly(2025, 3, 15), 1500m, -15000);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Monthly totals: Jan=10000, Feb=20000, Mar=15000 -> median=15000
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.True);
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(15000));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Not_Include_Transfers_In_Sat_Median()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add a real expense - Jan: 10000 sats
            AddExpenseTransactionWithSats(new DateOnly(2025, 1, 15), 1000m, -10000);

            // Add a transfer (has ToAmount) - should be excluded
            AddTransferTransaction(new DateOnly(2025, 2, 15), -5000, 5000);

            // Add another expense - Mar: 15000 sats
            AddExpenseTransactionWithSats(new DateOnly(2025, 3, 15), 1500m, -15000);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Only Jan and Mar should count: 10000, 15000 -> median=(10000+15000)/2=12500
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.True);
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(12500));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Not_Include_Income_In_Sat_Median()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        try
        {
            // Add an expense - Jan: 10000 sats
            AddExpenseTransactionWithSats(new DateOnly(2025, 1, 15), 1000m, -10000);

            // Add income (positive FromSatAmount) - should be excluded
            AddBtcIncomeTransaction(new DateOnly(2025, 2, 15), 50000);

            // Add another expense - Mar: 15000 sats
            AddExpenseTransactionWithSats(new DateOnly(2025, 3, 15), 1500m, -15000);

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
            var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

            var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

            // Only expenses count: Jan=10000, Mar=15000 -> median=(10000+15000)/2=12500
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.HasMedianMonthlyExpensesSats, Is.True);
                Assert.That(result.MedianMonthlyExpensesSats, Is.EqualTo(12500));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    #endregion

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

    private void AddExpenseTransactionWithSats(DateOnly date, decimal fiatAmount, long satAmount)
    {
        var autoSatDetails = new AutoSatAmountDetails(true, SatAmountState.Processed, BtcValue.New(Math.Abs(satAmount)));

        var entity = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = date,
            Name = $"Expense {date}",
            AutoSatAmountDetails = autoSatDetails,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), fiatAmount, false) // false = expense
        }.Build();

        // Override SatAmount to be negative for expenses (matching how the system stores them)
        entity.SatAmount = satAmount;

        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddBtcExpenseTransaction(DateOnly date, long satAmount)
    {
        var entity = new TransactionEntity
        {
            Id = new LiteDB.ObjectId(IdGenerator.Generate().ToString()),
            Type = TransactionEntityType.Bitcoin,
            Date = date.ToDateTime(TimeOnly.MinValue),
            Name = $"BTC Expense {date}",
            CategoryId = new LiteDB.ObjectId(_categoryId.ToString()),
            FromAccountId = _btcAccount.Id,
            FromSatAmount = satAmount, // Negative for expense
            ToAccountId = null,
            ToSatAmount = null,
            ToFiatAmount = null,
            Version = 0
        };

        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddBtcIncomeTransaction(DateOnly date, long satAmount)
    {
        var entity = new TransactionEntity
        {
            Id = new LiteDB.ObjectId(IdGenerator.Generate().ToString()),
            Type = TransactionEntityType.Bitcoin,
            Date = date.ToDateTime(TimeOnly.MinValue),
            Name = $"BTC Income {date}",
            CategoryId = new LiteDB.ObjectId(_categoryId.ToString()),
            FromAccountId = _btcAccount.Id,
            FromSatAmount = satAmount, // Positive for income
            ToAccountId = null,
            ToSatAmount = null,
            ToFiatAmount = null,
            Version = 0
        };

        _localDatabase.GetTransactions().Insert(entity);
    }

    private void AddTransferTransaction(DateOnly date, long fromSatAmount, long toSatAmount)
    {
        var entity = new TransactionEntity
        {
            Id = new LiteDB.ObjectId(IdGenerator.Generate().ToString()),
            Type = TransactionEntityType.Fiat,
            Date = date.ToDateTime(TimeOnly.MinValue),
            Name = $"Transfer {date}",
            CategoryId = new LiteDB.ObjectId(_categoryId.ToString()),
            FromAccountId = _brlAccount.Id,
            FromFiatAmount = -1000m,
            ToAccountId = _brlAccount.Id, // Transfer to same account for simplicity
            ToFiatAmount = 1000m,
            SatAmount = fromSatAmount,
            ToSatAmount = toSatAmount,
            Version = 0
        };

        _localDatabase.GetTransactions().Insert(entity);
    }
}
