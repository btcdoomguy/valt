using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Valt.App;
using Valt.App.Kernel.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Mcp.Tools;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.WealthOverview;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Mcp.Tools;

[TestFixture]
public class ReportToolsTests : IntegrationTest
{
    private IQueryDispatcher _queryDispatcher = null!;
    private IAssetRepository _assetRepository = null!;
    private IReportDataProviderFactory _reportDataProviderFactory = null!;
    private IAllTimeHighReport _allTimeHighReport = null!;
    private IWealthOverviewReport _wealthOverviewReport = null!;
    private CurrencySettings _currencySettings = null!;

    [OneTimeSetUp]
    public void AddApplicationLayer()
    {
        _serviceCollection.AddValtApp();
        RebuildServiceProvider();
    }

    [SetUp]
    public new void SetUp()
    {
        _queryDispatcher = _serviceProvider.GetRequiredService<IQueryDispatcher>();
        _assetRepository = _serviceProvider.GetRequiredService<IAssetRepository>();
        _reportDataProviderFactory = _serviceProvider.GetRequiredService<IReportDataProviderFactory>();
        _allTimeHighReport = _serviceProvider.GetRequiredService<IAllTimeHighReport>();
        _wealthOverviewReport = _serviceProvider.GetRequiredService<IWealthOverviewReport>();
        _currencySettings = _serviceProvider.GetRequiredService<CurrencySettings>();
        _currencySettings.MainFiatCurrency = FiatCurrency.Usd.Code;
    }

    [TearDown]
    public void TearDown()
    {
        _localDatabase.GetAccounts().DeleteAll();
        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetFixedExpenses().DeleteAll();
        _localDatabase.GetFixedExpenseRecords().DeleteAll();
        _localDatabase.GetAssets().DeleteAll();
    }

    [Test]
    public async Task GetSpendingAnalytics_WithCurrentMonthExpenseAndFixedExpense_ReturnsBurnRateAndFixedVsVariable()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        var account = new FiatAccountBuilder()
        {
            Name = "Checking",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(account);

        var categoryId = IdGenerator.Generate();

        var transaction = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = categoryId,
            Date = today.AddDays(-1),
            Name = "Current month expense",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(account.Id.ToString(), 500m, false)
        }.Build();
        _localDatabase.GetTransactions().Insert(transaction);

        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithCurrency(FiatCurrency.Usd)
            .WithName("Test Fixed Expense")
            .WithFixedAmountRange(100m, FixedExpensePeriods.Monthly, firstOfMonth, 1)
            .Build();
        _localDatabase.GetFixedExpenses().Insert(fixedExpense);

        var record = new FixedExpenseRecordEntity
        {
            Id = new ObjectId(),
            FixedExpense = fixedExpense,
            Transaction = transaction,
            FixedExpenseRecordStateId = (int)FixedExpenseRecordState.Paid,
            ReferenceDate = transaction.Date
        };
        _localDatabase.GetFixedExpenseRecords().Insert(record);

        SeedPriceData(today);

        var result = await ReportTools.GetSpendingAnalytics(
            _queryDispatcher,
            firstOfMonth.ToString("yyyy-MM-dd"),
            today.ToString("yyyy-MM-dd"),
            FiatCurrency.Usd.Code,
            10000m);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.BurnRate.HasData, Is.True);
            Assert.That(result.FixedVsVariable.Months, Is.Not.Empty);
            Assert.That(result.FixedVsVariable.Months.Any(m => m.Month == firstOfMonth.ToString("yyyy-MM-dd")), Is.True);
        }
    }

    [Test]
    public async Task GetBtcDenominatedMetrics_WithIncomeExpenseAndBtcPurchase_ReturnsNonEmptyMonths()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        var fiatAccount = new FiatAccountBuilder()
        {
            Name = "Checking",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(fiatAccount);

        var btcAccount = new BtcAccountBuilder()
        {
            Name = "BTC Wallet"
        }.WithSats(1_000_000).Build();
        _localDatabase.GetAccounts().Insert(btcAccount);

        var income = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            Date = today,
            TransactionDetails = new FiatDetails(fiatAccount.Id.ToString(), 2000m, true)
        }.Build();
        _localDatabase.GetTransactions().Insert(income);

        var expense = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            Date = today,
            TransactionDetails = new FiatDetails(fiatAccount.Id.ToString(), 300m, false)
        }.Build();
        _localDatabase.GetTransactions().Insert(expense);

        var purchase = new TransactionBuilder()
            .WithDate(today)
            .AsBitcoinPurchase(fiatAccount.Id.ToString(), btcAccount.Id.ToString(), 500_000, 500m)
            .Build();
        _localDatabase.GetTransactions().Insert(purchase);

        SeedPriceData(today);

        var result = await ReportTools.GetBtcDenominatedMetrics(
            _queryDispatcher,
            firstOfMonth.ToString("yyyy-MM-dd"),
            today.ToString("yyyy-MM-dd"),
            FiatCurrency.Usd.Code);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Months, Is.Not.Empty);
            Assert.That(result.IsEmpty, Is.False);
        }
    }

    [Test]
    public async Task GetLoanReports_WithActiveBtcLoan_ReturnsActiveLoanAndCostMonths()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var asset = AssetBuilder.ABtcLoan().WithSeededSnapshot().Build();
        await _assetRepository.SaveAsync(asset);

        var startDate = new DateOnly(today.Year, today.Month, 1).AddMonths(-2);
        var endDate = today.AddDays(-1);

        SeedPriceData(today);

        var result = await ReportTools.GetLoanReports(
            _queryDispatcher,
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd"),
            FiatCurrency.Usd.Code,
            100_000m);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.HasActiveLoans, Is.True);
            Assert.That(result.CostMonths, Is.Not.Empty);
            Assert.That(result.DistanceMonths, Is.Not.Empty);
        }
    }

    [Test]
    public async Task GetWealthPerformanceMetrics_WithTransactionData_ReturnsAthAndWealthOverview()
    {
        // This test verifies only the implemented v0.7 wealth-performance metrics:
        // days-under-water via the ATH report and monthly wealth overview.
        // CAGR, fiat-vs-BTC allocation, and best/worst months are not exposed.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        var account = new FiatAccountBuilder()
        {
            Name = "Checking",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(account);

        var transaction = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            Date = today.AddDays(-1),
            TransactionDetails = new FiatDetails(account.Id.ToString(), 500m, false)
        }.Build();
        _localDatabase.GetTransactions().Insert(transaction);

        SeedPriceData(today);

        var result = await ReportTools.GetWealthPerformanceMetrics(
            _reportDataProviderFactory,
            _allTimeHighReport,
            _wealthOverviewReport,
            FiatCurrency.Usd.Code);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllTimeHigh.DaysUnderWater, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.WealthOverview.Items, Is.Not.Empty);
        }
    }

    private void SeedPriceData(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        var btcCollection = _priceDatabase.GetBitcoinData();
        if (btcCollection.FindOne(x => x.Date == dateTime) is null)
            btcCollection.Insert(new BitcoinDataEntity { Date = dateTime, Price = 100000m });

        var fiatCollection = _priceDatabase.GetFiatData();
        if (fiatCollection.FindOne(x => x.Date == dateTime && x.Currency == FiatCurrency.Usd.Code) is null)
            fiatCollection.Insert(new FiatDataEntity { Date = dateTime, Currency = FiatCurrency.Usd.Code, Price = 1m });
    }
}
