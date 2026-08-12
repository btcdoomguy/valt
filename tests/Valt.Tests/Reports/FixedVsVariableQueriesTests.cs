using LiteDB;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Currency.Services;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.SpendingAnalytics.Queries;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class FixedVsVariableQueriesTests : DatabaseTest
{
    private AccountEntity _brlAccount = null!;
    private AccountEntity _eurAccount = null!;
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

        _eurAccount = new FiatAccountBuilder()
        {
            Name = "EUR Account",
            FiatCurrency = FiatCurrency.Eur,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_eurAccount);

        _btcAccount = new BtcAccountBuilder()
        {
            Name = "BTC Account",
            Value = BtcValue.New(100000000)
        }.Build();
        _localDatabase.GetAccounts().Insert(_btcAccount);

        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity { Date = currentDate, Price = 100000m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity { Date = currentDate, Currency = FiatCurrency.Brl.Code, Price = 5.5m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity { Date = currentDate, Currency = FiatCurrency.Eur.Code, Price = 0.75m });
            currentDate = currentDate.AddDays(1);
        }

        return base.SeedDatabase();
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [TearDown]
    public void TearDown()
    {
        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetFixedExpenseRecords().DeleteAll();
        _localDatabase.GetFixedExpenses().DeleteAll();
    }

    [Test]
    public async Task Should_Split_Fixed_And_Variable_When_Paid_Record_Bound()
    {
        var month = new DateOnly(2025, 1, 1);
        var fixedExpense = AddFixedExpense();
        var boundTransaction = AddBrlExpense(new DateOnly(2025, 1, 15), 300m);
        AddBrlExpense(new DateOnly(2025, 1, 20), 700m);
        AddFixedExpenseRecord(fixedExpense, boundTransaction, FixedExpenseRecordState.Paid);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonth(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.FixedTotal, Is.EqualTo(300m));
            Assert.That(monthData.VariableTotal, Is.EqualTo(700m));
        }
    }

    [Test]
    public async Task Should_Only_Count_Paid_State_Toward_Fixed()
    {
        var month = new DateOnly(2025, 1, 1);
        var fixedExpense = AddFixedExpense();
        var paidTransaction = AddBrlExpense(new DateOnly(2025, 1, 10), 300m);
        var manuallyPaidTransaction = AddBrlExpense(new DateOnly(2025, 1, 15), 200m);
        var ignoredTransaction = AddBrlExpense(new DateOnly(2025, 1, 20), 200m);
        var emptyTransaction = AddBrlExpense(new DateOnly(2025, 1, 25), 200m);

        AddFixedExpenseRecord(fixedExpense, paidTransaction, FixedExpenseRecordState.Paid);
        AddFixedExpenseRecord(fixedExpense, manuallyPaidTransaction, FixedExpenseRecordState.ManuallyPaid);
        AddFixedExpenseRecord(fixedExpense, ignoredTransaction, FixedExpenseRecordState.Ignored);
        AddFixedExpenseRecord(fixedExpense, emptyTransaction, FixedExpenseRecordState.Empty);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonth(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.FixedTotal, Is.EqualTo(300m));
            Assert.That(monthData.VariableTotal, Is.EqualTo(600m));
        }
    }

    [Test]
    public async Task Should_Return_All_Variable_When_No_Bound_Records()
    {
        var month = new DateOnly(2025, 1, 1);
        AddFixedExpense();
        AddBrlExpense(new DateOnly(2025, 1, 15), 500m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonth(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.FixedTotal, Is.EqualTo(0m));
            Assert.That(monthData.VariableTotal, Is.EqualTo(500m));
        }
    }

    [Test]
    public async Task Should_Include_Current_Incomplete_Month()
    {
        var clock = new FakeClock(new DateTime(2025, 3, 15));
        var fixedExpense = AddFixedExpense();
        var februaryTransaction = AddBrlExpense(new DateOnly(2025, 2, 15), 300m);
        AddBrlExpense(new DateOnly(2025, 3, 10), 200m);
        AddFixedExpenseRecord(fixedExpense, februaryTransaction, FixedExpenseRecordState.Paid);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), clock);

        var february = GetMonth(result, new DateOnly(2025, 2, 1));
        var march = GetMonth(result, new DateOnly(2025, 3, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Months, Has.Count.EqualTo(12));
            Assert.That(february, Is.Not.Null);
            Assert.That(february!.FixedTotal, Is.EqualTo(300m));
            Assert.That(february.VariableTotal, Is.EqualTo(0m));
            Assert.That(march, Is.Not.Null);
            Assert.That(march!.FixedTotal, Is.EqualTo(0m));
            Assert.That(march.VariableTotal, Is.EqualTo(200m));
        }
    }

    [Test]
    public async Task Should_Flag_No_Fixed_Expenses_When_None_Registered()
    {
        AddBrlExpense(new DateOnly(2025, 1, 15), 500m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasNoFixedExpenses, Is.True);
            Assert.That(result.Months, Is.Empty);
        }
    }

    [Test]
    public async Task Should_Convert_Secondary_Currency_To_Main_Fiat()
    {
        var month = new DateOnly(2025, 1, 1);
        AddFixedExpense();
        AddEurExpense(new DateOnly(2025, 1, 15), 75m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));
        var monthData = GetMonth(result, month);

        // EUR 75 -> USD 100 -> BRL 550
        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.FixedTotal, Is.EqualTo(0m));
            Assert.That(monthData.VariableTotal, Is.EqualTo(550m));
        }
    }

    [Test]
    public async Task Should_Return_All_Months_When_No_Expense_Transactions_In_Range()
    {
        AddFixedExpense();

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasNoFixedExpenses, Is.False);
            Assert.That(result.Months, Has.Count.EqualTo(24));
            Assert.That(result.Months.All(m => m.FixedTotal == 0m && m.VariableTotal == 0m), Is.True);
        }
    }

    private static FixedVsVariableMonthDto? GetMonth(FixedVsVariableDataDto result, DateOnly month)
    {
        return result.Months.FirstOrDefault(x => x.Month == month);
    }

    [Test]
    public async Task Should_Reduce_Fixed_Total_When_Category_Filter_Includes_Only_One_Category()
    {
        var categoryA = IdGenerator.Generate();
        var categoryB = IdGenerator.Generate();

        var month = new DateOnly(2025, 1, 1);
        var fixedExpense = AddFixedExpense();
        var transactionA = AddBrlExpense(new DateOnly(2025, 1, 10), 300m, categoryA);
        var transactionB = AddBrlExpense(new DateOnly(2025, 1, 15), 700m, categoryB);
        AddFixedExpenseRecord(fixedExpense, transactionA, FixedExpenseRecordState.Paid);
        AddFixedExpenseRecord(fixedExpense, transactionB, FixedExpenseRecordState.Paid);

        var withBoth = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31),
            [categoryA.ToString(), categoryB.ToString()]);
        var withOnlyA = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31),
            [categoryA.ToString()]);

        var bothMonth = GetMonth(withBoth, month);
        var onlyAMonth = GetMonth(withOnlyA, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bothMonth, Is.Not.Null);
            Assert.That(bothMonth!.FixedTotal, Is.EqualTo(1000m));
            Assert.That(onlyAMonth, Is.Not.Null);
            Assert.That(onlyAMonth!.FixedTotal, Is.EqualTo(300m));
        }
    }

    private async Task<FixedVsVariableDataDto> ExecuteQuery(DateOnly from, DateOnly to, FakeClock clock)
    {
        return await ExecuteQuery(from, to, clock, []);
    }

    private async Task<FixedVsVariableDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), []);
    }

    private async Task<FixedVsVariableDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate, string[] categoryIds)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), categoryIds);
    }

    private async Task<FixedVsVariableDataDto> ExecuteQuery(DateOnly from, DateOnly to, FakeClock clock, string[] categoryIds)
    {
        var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        };
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);
        var sut = new FixedVsVariableQueries(
            _localDatabase,
            factory,
            currencySettings);

        return await sut.GetFixedVsVariableAsync(new GetFixedVsVariableQuery
        {
            From = from,
            To = to,
            AccountIds = [_brlAccount.Id.ToString(), _eurAccount.Id.ToString(), _btcAccount.Id.ToString()],
            CategoryIds = categoryIds
        });
    }

    private FixedExpenseEntity AddFixedExpense()
    {
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithCurrency(FiatCurrency.Brl)
            .WithName("Test Fixed Expense")
            .WithFixedAmountRange(100m, FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 1)
            .Build();
        _localDatabase.GetFixedExpenses().Insert(fixedExpense);
        return fixedExpense;
    }

    private TransactionEntity AddBrlExpense(DateOnly date, decimal amount, CategoryId categoryId)
    {
        var entity = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = categoryId,
            Date = date,
            Name = $"BRL Expense {date}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, false)
        }.Build();
        _localDatabase.GetTransactions().Insert(entity);
        return entity;
    }

    private TransactionEntity AddBrlExpense(DateOnly date, decimal amount)
    {
        return AddBrlExpense(date, amount, _categoryId);
    }

    private TransactionEntity AddEurExpense(DateOnly date, decimal amount)
    {
        var entity = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = date,
            Name = $"EUR Expense {date}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_eurAccount.Id.ToString(), amount, false)
        }.Build();
        _localDatabase.GetTransactions().Insert(entity);
        return entity;
    }

    private void AddFixedExpenseRecord(FixedExpenseEntity fixedExpense, TransactionEntity transaction, FixedExpenseRecordState state)
    {
        var record = new FixedExpenseRecordEntity
        {
            Id = new ObjectId(),
            FixedExpense = fixedExpense,
            Transaction = transaction,
            FixedExpenseRecordStateId = (int)state,
            ReferenceDate = transaction.Date
        };
        _localDatabase.GetFixedExpenseRecords().Insert(record);
    }
}
