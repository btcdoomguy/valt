using System.Reflection;
using LiteDB;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.App.Modules.SpendingEvolution.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.SpendingAnalytics.Queries;
using Valt.Infra.Modules.SpendingEvolution.Queries;
using Valt.Infra.Settings;
using Valt.Tests.Builders;
using TransactionEntity = Valt.Infra.Modules.Budget.Transactions.TransactionEntity;

namespace Valt.Tests.Reports;

/// <summary>
/// GAP-2 characterization: Fixed vs Variable and Spending Evolution are the only two report
/// queries that filter LiteDB-side (.Query().Where). LiteDB translates "!x.IsExcluded" into the
/// engine expression "($.excl = false)"; documents written before phase 53 lack the "excl" BSON
/// field, the engine reads the missing field as NULL, and "NULL = false" evaluates false — the
/// row is silently dropped. Real databases are dominated by such legacy rows, so the panels
/// collapse toward empty/zero. These tests seed documents through the raw-insert path with the
/// "excl" field stripped, reproducing the defect deterministically. A document with "excl"
/// explicitly false must still be counted (guard against over-correction).
/// </summary>
[TestFixture]
public class ReportExclusionLegacyDocumentTests : DatabaseTest
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

        PriceDataBuilder.SeedRange(_priceDatabase, new DateTime(2024, 1, 1), new DateTime(2025, 12, 31), 100000m,
            (FiatCurrency.Brl.Code, 5.5m), (FiatCurrency.Eur.Code, 0.75m));

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
    public async Task FixedVsVariable_Legacy_Paid_Bound_Expense_Aggregates_As_Fixed()
    {
        var month = new DateOnly(2025, 1, 1);
        var fixedExpense = AddFixedExpense();

        // Legacy document: bound to a Paid fixed-expense record, but its BSON document
        // lacks the "excl" field (written before phase 53). Must count toward FixedTotal.
        var legacyPaid = AddBrlExpenseEntity(new DateOnly(2025, 1, 10), 300m);
        InsertLegacyDocument(legacyPaid);
        AddFixedExpenseRecord(fixedExpense, legacyPaid, FixedExpenseRecordState.Paid);

        AddBrlExpense(new DateOnly(2025, 1, 20), 700m);
        AddBrlExpense(new DateOnly(2025, 1, 25), 999m, isExcluded: true);

        var result = await ExecuteFixedVsVariableQuery();
        var monthData = GetMonth(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.FixedTotal, Is.EqualTo(300m));
            Assert.That(monthData.VariableTotal, Is.EqualTo(700m));
        }
    }

    [Test]
    public async Task FixedVsVariable_Legacy_RealData_Mix_Aggregates_As_Expected()
    {
        var month = new DateOnly(2025, 1, 1);
        var fixedExpense = AddFixedExpense();

        // Legacy sat-only Bitcoin expense: 100_000 sats -> BRL 550 (variable)
        InsertLegacyDocument(AddBitcoinExpenseEntity(new DateOnly(2025, 1, 5), 100_000));

        // Legacy multi-currency (EUR) expense: EUR 75 -> USD 100 -> BRL 550 (variable)
        InsertLegacyDocument(AddEurExpenseEntity(new DateOnly(2025, 1, 8), 75m));

        // Legacy BRL expense bound to a Paid record (fixed)
        var legacyPaid = AddBrlExpenseEntity(new DateOnly(2025, 1, 12), 300m);
        InsertLegacyDocument(legacyPaid);
        AddFixedExpenseRecord(fixedExpense, legacyPaid, FixedExpenseRecordState.Paid);

        var result = await ExecuteFixedVsVariableQuery();
        var monthData = GetMonth(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.FixedTotal, Is.EqualTo(300m));
            Assert.That(monthData.VariableTotal, Is.EqualTo(1100m));
        }
    }

    [Test]
    public async Task SpendingEvolution_Legacy_Docs_Included_Modern_Excluded_Excluded()
    {
        InsertLegacyDocument(AddBrlExpenseEntity(new DateOnly(2025, 1, 15), 300m));
        AddBrlExpense(new DateOnly(2025, 1, 20), 200m);
        AddBrlExpense(new DateOnly(2025, 1, 25), 700m, isExcluded: true);

        var result = await ExecuteSpendingEvolutionQuery();
        var january = result.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.FiatTotal, Is.EqualTo(500m));
            Assert.That(january.TransactionCount, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task Modern_Document_With_Explicit_False_Excl_Is_Counted()
    {
        var month = new DateOnly(2025, 1, 1);
        AddFixedExpense();

        // excl physically present and false: must be counted both before and after the fix
        // (guards against over-correction treating explicit-false as excluded).
        AddBrlExpense(new DateOnly(2025, 1, 15), 400m, isExcluded: false);

        var result = await ExecuteFixedVsVariableQuery();
        var monthData = GetMonth(result, month);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monthData, Is.Not.Null);
            Assert.That(monthData!.FixedTotal, Is.EqualTo(0m));
            Assert.That(monthData.VariableTotal, Is.EqualTo(400m));
        }
    }

    private async Task<FixedVsVariableDataDto> ExecuteFixedVsVariableQuery()
    {
        var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        };
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, new FakeClock(new DateTime(2025, 12, 31)));
        var sut = new FixedVsVariableQueries(_localDatabase, factory, currencySettings);

        return await sut.GetFixedVsVariableAsync(new GetFixedVsVariableQuery
        {
            From = new DateOnly(2024, 1, 1),
            To = new DateOnly(2025, 12, 31),
            AccountIds = [_brlAccount.Id.ToString(), _eurAccount.Id.ToString(), _btcAccount.Id.ToString()],
            CategoryIds = []
        });
    }

    private async Task<Valt.App.Modules.SpendingEvolution.DTOs.SpendingEvolutionDataDto> ExecuteSpendingEvolutionQuery()
    {
        var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        };
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, new FakeClock(new DateTime(2025, 12, 31)));
        var sut = new SpendingEvolutionQueries(_localDatabase, factory, currencySettings);

        return await sut.GetSpendingEvolutionAsync(new GetSpendingEvolutionQuery
        {
            From = new DateOnly(2024, 1, 1),
            To = new DateOnly(2025, 12, 31),
            AccountIds = [_brlAccount.Id.ToString(), _eurAccount.Id.ToString(), _btcAccount.Id.ToString()],
            CategoryIds = []
        });
    }

    private static FixedVsVariableMonthDto? GetMonth(FixedVsVariableDataDto result, DateOnly month)
    {
        return result.Months.FirstOrDefault(x => x.Month == month);
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

    private TransactionEntity AddBrlExpenseEntity(DateOnly date, decimal amount, bool isExcluded = false)
    {
        return new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = date,
            Name = $"BRL Expense {date}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, false),
            IsExcluded = isExcluded
        }.Build();
    }

    private TransactionEntity AddEurExpenseEntity(DateOnly date, decimal amount)
    {
        return new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = date,
            Name = $"EUR Expense {date}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_eurAccount.Id.ToString(), amount, false)
        }.Build();
    }

    private TransactionEntity AddBitcoinExpenseEntity(DateOnly date, long satAmount)
    {
        return new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = date,
            Name = $"BTC Expense {date}",
            AutoSatAmountDetails = null,
            TransactionDetails = new BitcoinDetails(_btcAccount.Id.ToString(), BtcValue.ParseSats(satAmount), credit: false)
        }.Build();
    }

    private TransactionEntity AddBrlExpense(DateOnly date, decimal amount, bool isExcluded = false)
    {
        var entity = AddBrlExpenseEntity(date, amount, isExcluded);
        _localDatabase.GetTransactions().Insert(entity);
        return entity;
    }

    /// <summary>
    /// Inserts a transaction document directly into the raw LiteDB collection with the "excl"
    /// field stripped, simulating a document written before the exclusion flag existed.
    /// Uses the app's own BsonMapper so field names match production serialization.
    /// </summary>
    private TransactionEntity InsertLegacyDocument(TransactionEntity entity)
    {
        var database = GetUnderlyingDatabase();

        var document = database.Mapper.ToDocument(entity);
        document.Remove("excl");

        database.GetCollection("budget_transactions").Insert(document);

        return entity;
    }

    private LiteDatabase GetUnderlyingDatabase()
    {
        var field = typeof(LocalDatabase).GetField("_database",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (LiteDatabase)field!.GetValue(_localDatabase)!;
    }
}
