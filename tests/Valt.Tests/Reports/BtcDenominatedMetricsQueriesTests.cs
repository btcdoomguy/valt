using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
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
    private AccountEntity _usdAccount = null!;
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

        _usdAccount = new FiatAccountBuilder()
        {
            Name = "USD Account",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        _btcAccount = new BtcAccountBuilder()
            .WithName("BTC Account")
            .WithSats(1_000_000)
            .Build();
        _localDatabase.GetAccounts().Insert(_btcAccount);

        PriceDataBuilder.SeedRange(_priceDatabase, new DateTime(2024, 1, 1), new DateTime(2025, 12, 31), 100000m,
            (FiatCurrency.Brl.Code, 5.5m), (FiatCurrency.Usd.Code, 1m));

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
            Assert.That(monthData.StackVelocity, Is.EqualTo(monthData.SatsEarned + monthData.SatsSpent));
        }
    }

    [Test]
    public async Task Should_Convert_Fiat_Income_And_Expense_To_Sats_Per_Month()
    {
        var month = new DateOnly(2025, 1, 1);
        AddIncomeTransaction(month, 1100m);
        AddExpenseTransaction(month, 550m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonthData(result, month);

        // 1100 BRL / 5.5 = 200 USD; 200 / 100000 = 0.002 BTC = 200000 sats
        Assert.That(monthData!.SatsEarned, Is.EqualTo(200000L));
        // 550 BRL / 5.5 = 100 USD; 100 / 100000 = 0.001 BTC = 100000 sats (stored as negative)
        Assert.That(monthData.SatsSpent, Is.EqualTo(-100000L));
    }

    [Test]
    public async Task Should_Include_Native_Bitcoin_Income_And_Expense_By_Sign()
    {
        var month = new DateOnly(2025, 1, 1);
        AddBtcIncomeTransaction(month, 500000L);
        AddBtcExpenseTransaction(month, 250000L);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonthData(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData!.SatsEarned, Is.EqualTo(500000L));
            Assert.That(monthData.SatsSpent, Is.EqualTo(-250000L));
        }
    }

    [Test]
    public async Task Should_Exclude_Btc_Purchases_And_Sales_From_Earned_And_Spent()
    {
        var month = new DateOnly(2025, 1, 1);
        AddBtcPurchaseTransaction(month, 100000L, 100m);
        AddBtcSaleTransaction(month, 50000L, 50m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonthData(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData!.SatsEarned, Is.EqualTo(0));
            Assert.That(monthData.SatsSpent, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Should_Include_Btc_Purchases_And_Sales_In_Stack_Velocity()
    {
        var month = new DateOnly(2025, 1, 1);
        AddBtcPurchaseTransaction(month, 100000L, 100m);
        AddBtcSaleTransaction(month, 50000L, 50m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonthData(result, month);

        // velocity = 0 + 0 + 100000 + (-50000) = 50000
        Assert.That(monthData!.StackVelocity, Is.EqualTo(50000L));
    }

    [Test]
    public async Task Should_Exclude_Internal_Transfers()
    {
        var month = new DateOnly(2025, 1, 1);
        AddInternalTransferTransaction(month, 500m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonthData(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.SatsEarned, Is.EqualTo(0));
            Assert.That(monthData.SatsSpent, Is.EqualTo(0));
            Assert.That(monthData.StackVelocity, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Should_Skip_Missing_Btc_Rate_Date()
    {
        var month = new DateOnly(2025, 1, 1);
        var transactionDate = new DateTime(2025, 1, 10);
        AddIncomeTransaction(month, 1000m, transactionDate.Day);

        var entity = _priceDatabase.GetBitcoinData().FindOne(x => x.Date == transactionDate);
        Assert.That(entity, Is.Not.Null);
        _priceDatabase.GetBitcoinData().Delete(entity.Id);

        try
        {
            var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
            var monthData = GetMonthData(result, month);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(monthData, Is.Not.Null);
                Assert.That(monthData!.SatsEarned, Is.EqualTo(0));
                Assert.That(monthData.SatsSpent, Is.EqualTo(0));
            }
        }
        finally
        {
            // Restore the shared fixture price row so later tests in this fixture
            // do not run against mutated price data
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity { Date = transactionDate, Price = 100000m });
        }
    }

    [Test]
    public async Task Should_Include_Current_Incomplete_Month()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 15));
        AddIncomeTransaction(new DateOnly(2025, 3, 1), 1000m, 10);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), clock);
        var monthData = GetMonthData(result, new DateOnly(2025, 3, 1));

        Assert.That(monthData, Is.Not.Null);
    }

    [Test]
    public async Task Should_Return_Zero_Velocity_Months_On_Baseline()
    {
        AddIncomeTransaction(new DateOnly(2025, 1, 1), 1000m, 10);
        AddIncomeTransaction(new DateOnly(2025, 3, 1), 500m, 10);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonthData(result, new DateOnly(2025, 2, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.StackVelocity, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Should_Honor_Category_Exclusion()
    {
        var categoryA = InsertCategory("Category A");
        var categoryB = InsertCategory("Category B");

        var month = new DateOnly(2025, 1, 1);
        AddIncomeTransaction(month, 1000m, 10, categoryA);
        AddExpenseTransaction(month, 500m, 15, categoryB);

        var result = await ExecuteQuery(
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 12, 31),
            new FakeClock(new DateTime(2025, 12, 31)),
            [categoryA.ToString()]);

        var monthData = GetMonthData(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData!.SatsEarned, Is.GreaterThan(0));
            Assert.That(monthData.SatsSpent, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Should_Honor_Account_Filter()
    {
        var month = new DateOnly(2025, 1, 1);
        AddIncomeTransaction(month, 1000m, 10, new AccountId(_brlAccount.Id.ToString()));
        AddIncomeTransaction(month, 2000m, 11, new AccountId(_usdAccount.Id.ToString()));

        var result = await ExecuteQuery(
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 12, 31),
            new FakeClock(new DateTime(2025, 12, 31)),
            [],
            [_brlAccount.Id.ToString()]);

        var monthData = GetMonthData(result, month);

        // 1000 BRL / 5.5 BRL/USD = 181.82 USD; 181.82 USD / 100000 USD/BTC = 0.0000018182 BTC = 181818 sats
        Assert.That(monthData!.SatsEarned, Is.EqualTo(181818L));
    }

    private static BtcDenominatedMetricsMonthDto? GetMonthData(BtcDenominatedMetricsDataDto result, DateOnly month)
    {
        return result.Months.FirstOrDefault(x => x.Month == month);
    }

    private async Task<BtcDenominatedMetricsDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), [], []);
    }

    private async Task<BtcDenominatedMetricsDataDto> ExecuteQuery(DateOnly from, DateOnly to, FakeClock clock)
    {
        return await ExecuteQuery(from, to, clock, [], []);
    }

    private async Task<BtcDenominatedMetricsDataDto> ExecuteQuery(
        DateOnly from,
        DateOnly to,
        FakeClock clock,
        string[] categoryIds)
    {
        return await ExecuteQuery(from, to, clock, categoryIds, []);
    }

    private async Task<BtcDenominatedMetricsDataDto> ExecuteQuery(
        DateOnly from,
        DateOnly to,
        FakeClock clock,
        string[] categoryIds,
        string[] accountIds)
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
            AccountIds = accountIds,
            CategoryIds = categoryIds
        });
    }

    private CategoryId InsertCategory(string name, Icon? icon = null)
    {
        var id = IdGenerator.Generate();
        _localDatabase.GetCategories().Insert(CategoryBuilder.ACategory()
            .WithId(id)
            .WithName(name)
            .WithIcon(icon ?? Icon.Empty)
            .Build());
        return id;
    }

    private void AddIncomeTransaction(DateOnly month, decimal amount, int day = 10)
    {
        AddIncomeTransaction(month, amount, day, _categoryId, _brlAccount.Id.ToString());
    }

    private void AddIncomeTransaction(DateOnly month, decimal amount, int day, CategoryId categoryId)
    {
        AddIncomeTransaction(month, amount, day, categoryId, _brlAccount.Id.ToString());
    }

    private void AddIncomeTransaction(DateOnly month, decimal amount, int day, AccountId accountId)
    {
        AddIncomeTransaction(month, amount, day, _categoryId, accountId);
    }

    private void AddIncomeTransaction(DateOnly month, decimal amount, int day, CategoryId categoryId, AccountId accountId)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = categoryId,
            Date = new DateOnly(month.Year, month.Month, day),
            Name = $"Income {month}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(accountId, amount, true)
        }.Build());
    }

    private void AddExpenseTransaction(DateOnly month, decimal amount, int day = 15)
    {
        AddExpenseTransaction(month, amount, day, _categoryId, _brlAccount.Id.ToString());
    }

    private void AddExpenseTransaction(DateOnly month, decimal amount, int day, CategoryId categoryId)
    {
        AddExpenseTransaction(month, amount, day, categoryId, _brlAccount.Id.ToString());
    }

    private void AddExpenseTransaction(DateOnly month, decimal amount, int day, CategoryId categoryId, AccountId accountId)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = categoryId,
            Date = new DateOnly(month.Year, month.Month, day),
            Name = $"Expense {month}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(accountId, amount, false)
        }.Build());
    }

    private void AddBtcIncomeTransaction(DateOnly month, long satAmount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            .WithId(IdGenerator.Generate())
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(month.Year, month.Month, 10))
            .WithName($"BTC Income {month}")
            .WithAutoSatAmountDetails(null)
            .WithTransactionDetails(new BitcoinDetails(_btcAccount.Id.ToString(), satAmount, credit: true))
            .Build());
    }

    private void AddBtcExpenseTransaction(DateOnly month, long satAmount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            .WithId(IdGenerator.Generate())
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(month.Year, month.Month, 10))
            .WithName($"BTC Expense {month}")
            .WithAutoSatAmountDetails(null)
            .WithTransactionDetails(new BitcoinDetails(_btcAccount.Id.ToString(), satAmount, credit: false))
            .Build());
    }

    private void AddBtcPurchaseTransaction(DateOnly month, long satAmount, decimal fiatAmount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            .WithId(IdGenerator.Generate())
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(month.Year, month.Month, 10))
            .WithName($"BTC Purchase {month}")
            .WithAutoSatAmountDetails(null)
            .WithTransactionDetails(new FiatToBitcoinDetails(_brlAccount.Id.ToString(), _btcAccount.Id.ToString(), fiatAmount, satAmount))
            .Build());
    }

    private void AddBtcSaleTransaction(DateOnly month, long satAmount, decimal fiatAmount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            .WithId(IdGenerator.Generate())
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(month.Year, month.Month, 10))
            .WithName($"BTC Sale {month}")
            .WithAutoSatAmountDetails(null)
            .WithTransactionDetails(new BitcoinToFiatDetails(_btcAccount.Id.ToString(), _brlAccount.Id.ToString(), satAmount, fiatAmount))
            .Build());
    }

    private void AddInternalTransferTransaction(DateOnly month, decimal amount)
    {
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            .WithId(IdGenerator.Generate())
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(month.Year, month.Month, 10))
            .WithName($"Transfer {month}")
            .WithAutoSatAmountDetails(null)
            .WithTransactionDetails(new FiatToFiatDetails(_brlAccount.Id.ToString(), _usdAccount.Id.ToString(), amount, amount))
            .Build());
    }
}
