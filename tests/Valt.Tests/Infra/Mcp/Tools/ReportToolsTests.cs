using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Valt.App;
using Valt.App.Kernel.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
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
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Mcp.Tools;

[TestFixture]
public class ReportToolsTests : IntegrationTest
{
    private IQueryDispatcher _queryDispatcher = null!;
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
        _currencySettings = _serviceProvider.GetRequiredService<CurrencySettings>();
        _currencySettings.MainFiatCurrency = FiatCurrency.Usd.Code;
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

    private void SeedPriceData(DateOnly today)
    {
        var dateTime = today.ToDateTime(TimeOnly.MinValue);
        _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity { Date = dateTime, Price = 100000m });
        _priceDatabase.GetFiatData().Insert(new FiatDataEntity { Date = dateTime, Currency = FiatCurrency.Usd.Code, Price = 1m });
    }
}
