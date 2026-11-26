using System.Reflection;
using LiteDB;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.SpendingEvolution.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.SpendingEvolution.Queries;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class SpendingEvolutionQueriesTests : DatabaseTest
{
    private AccountEntity _brlAccount = null!;
    private AccountEntity _eurAccount = null!;
    private CategoryId _categoryId = null!;
    private CategoryId _secondCategoryId = null!;

    protected override Task SeedDatabase()
    {
        _categoryId = IdGenerator.Generate();
        _secondCategoryId = IdGenerator.Generate();

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
            Value = FiatValue.New(5000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_eurAccount);

        PriceDataBuilder.SeedRange(_priceDatabase, new DateTime(2024, 1, 1), new DateTime(2025, 12, 31), 100000m,
            (FiatCurrency.Brl.Code, 5.5m),
            (FiatCurrency.Eur.Code, 0.75m));

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
    }

    [Test]
    public async Task Should_Aggregate_Unflagged_Expenses_By_Month()
    {
        AddBrlExpense(new DateOnly(2025, 1, 15), 300m);
        AddBrlExpense(new DateOnly(2025, 1, 20), 200m);
        AddBrlExpense(new DateOnly(2025, 2, 10), 100m);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31));

        var january = result.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));
        var february = result.Months.Single(x => x.Month == new DateOnly(2025, 2, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.FiatTotal, Is.EqualTo(500m));
            Assert.That(january.TransactionCount, Is.EqualTo(2));
            Assert.That(february.FiatTotal, Is.EqualTo(100m));
            Assert.That(february.TransactionCount, Is.EqualTo(1));
        }
    }

    #region Exclusion Flag Tests

    [Test]
    public async Task Flagged_Expense_Is_Excluded_From_Monthly_Series()
    {
        AddBrlExpense(new DateOnly(2025, 1, 15), 300m);
        AddBrlExpense(new DateOnly(2025, 1, 20), 700m, isExcluded: true);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31));

        var january = result.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.FiatTotal, Is.EqualTo(300m));
            Assert.That(january.TransactionCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Legacy_Doc_Counted_And_Only_Flagged_Omitted_In_Mixed_Month()
    {
        InsertLegacyDocument(AddBrlExpenseEntity(new DateOnly(2025, 1, 10), 200m));
        AddBrlExpense(new DateOnly(2025, 1, 15), 300m);
        AddBrlExpense(new DateOnly(2025, 1, 20), 700m, isExcluded: true);

        var result = await ExecuteQuery(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31));

        var january = result.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.FiatTotal, Is.EqualTo(500m));
            Assert.That(january.TransactionCount, Is.EqualTo(2));
        }
    }

    #endregion

    #region Filter Tests

    [Test]
    public async Task Should_Filter_Expenses_By_Selected_Category()
    {
        AddBrlExpense(new DateOnly(2025, 1, 15), 300m, categoryId: _categoryId);
        AddBrlExpense(new DateOnly(2025, 2, 10), 100m, categoryId: _categoryId);
        AddBrlExpense(new DateOnly(2025, 1, 20), 700m, categoryId: _secondCategoryId);
        AddBrlExpense(new DateOnly(2025, 2, 15), 400m, categoryId: _secondCategoryId);

        var resultA = await ExecuteQuery(
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 12, 31),
            categoryIds: [_categoryId.Value]);

        var resultB = await ExecuteQuery(
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 12, 31),
            categoryIds: [_secondCategoryId.Value]);

        var janA = resultA.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));
        var febA = resultA.Months.Single(x => x.Month == new DateOnly(2025, 2, 1));
        var janB = resultB.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));
        var febB = resultB.Months.Single(x => x.Month == new DateOnly(2025, 2, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(janA.FiatTotal, Is.EqualTo(300m));
            Assert.That(janA.TransactionCount, Is.EqualTo(1));
            Assert.That(febA.FiatTotal, Is.EqualTo(100m));
            Assert.That(febA.TransactionCount, Is.EqualTo(1));

            Assert.That(janB.FiatTotal, Is.EqualTo(700m));
            Assert.That(janB.TransactionCount, Is.EqualTo(1));
            Assert.That(febB.FiatTotal, Is.EqualTo(400m));
            Assert.That(febB.TransactionCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Should_Filter_Expenses_By_Selected_Account()
    {
        AddBrlExpense(new DateOnly(2025, 1, 15), 300m);
        // 75 EUR -> 75 / 0.75 = 100 USD -> 100 * 5.5 = 550 BRL
        AddExpense(_eurAccount, new DateOnly(2025, 1, 20), 75m, _categoryId);

        var brlOnly = await ExecuteQuery(
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 12, 31),
            accountIds: [_brlAccount.Id.ToString()]);

        var eurOnly = await ExecuteQuery(
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 12, 31),
            accountIds: [_eurAccount.Id.ToString()]);

        var bothAccounts = await ExecuteQuery(
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 12, 31),
            accountIds: [_brlAccount.Id.ToString(), _eurAccount.Id.ToString()]);

        using (Assert.EnterMultipleScope())
        {
            var janBrl = brlOnly.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));
            Assert.That(janBrl.FiatTotal, Is.EqualTo(300m));
            Assert.That(janBrl.TransactionCount, Is.EqualTo(1));

            var janEur = eurOnly.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));
            Assert.That(janEur.FiatTotal, Is.EqualTo(550m));
            Assert.That(janEur.TransactionCount, Is.EqualTo(1));

            var janBoth = bothAccounts.Months.Single(x => x.Month == new DateOnly(2025, 1, 1));
            Assert.That(janBoth.FiatTotal, Is.EqualTo(850m));
            Assert.That(janBoth.TransactionCount, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task Should_Filter_By_Combined_Account_And_Category_And_DateRange()
    {
        AddExpense(_brlAccount, new DateOnly(2024, 11, 10), 200m, _categoryId, satAmount: 40000L);
        AddExpense(_brlAccount, new DateOnly(2025, 1, 15), 300m, _categoryId, satAmount: 60000L);
        AddExpense(_brlAccount, new DateOnly(2025, 1, 20), 700m, _secondCategoryId, satAmount: 140000L);
        AddExpense(_eurAccount, new DateOnly(2025, 1, 25), 75m, _categoryId, satAmount: 100000L);
        AddExpense(_brlAccount, new DateOnly(2025, 3, 5), 450m, _categoryId, satAmount: 90000L);

        var filtered = await ExecuteQuery(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 2, 28),
            accountIds: [_brlAccount.Id.ToString()],
            categoryIds: [_categoryId.Value]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(filtered.Months, Has.Count.EqualTo(1));
            var jan = filtered.Months[0];
            Assert.That(jan.Month, Is.EqualTo(new DateOnly(2025, 1, 1)));
            Assert.That(jan.FiatTotal, Is.EqualTo(300m));
            Assert.That(jan.SatsTotal, Is.EqualTo(60000L));
            Assert.That(jan.TransactionCount, Is.EqualTo(1));
        }
    }

    #endregion

    private async Task<Valt.App.Modules.SpendingEvolution.DTOs.SpendingEvolutionDataDto> ExecuteQuery(
        DateOnly from,
        DateOnly to,
        string[]? accountIds = null,
        string[]? categoryIds = null)
    {
        var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        };
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, new FakeClock(new DateTime(2025, 12, 31)));
        var sut = new SpendingEvolutionQueries(_localDatabase, factory, currencySettings);

        return await sut.GetSpendingEvolutionAsync(new GetSpendingEvolutionQuery
        {
            From = from,
            To = to,
            AccountIds = accountIds ?? [_brlAccount.Id.ToString()],
            CategoryIds = categoryIds ?? []
        });
    }

    private TransactionEntity AddBrlExpense(DateOnly date, decimal amount, bool isExcluded = false, CategoryId? categoryId = null)
    {
        return AddExpense(_brlAccount, date, amount, categoryId ?? _categoryId, isExcluded);
    }

    private TransactionEntity AddExpense(
        AccountEntity account,
        DateOnly date,
        decimal amount,
        CategoryId categoryId,
        bool isExcluded = false,
        long? satAmount = null)
    {
        var entity = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = categoryId,
            Date = date,
            Name = $"Expense {date}",
            AutoSatAmountDetails = satAmount.HasValue
                ? new AutoSatAmountDetails(true, SatAmountState.Processed, BtcValue.ParseSats(satAmount.Value))
                : AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(account.Id.ToString(), amount, false),
            IsExcluded = isExcluded
        }.Build();
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

    private LiteDatabase GetUnderlyingDatabase()
    {
        var field = typeof(Valt.Infra.DataAccess.LocalDatabase).GetField("_database",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (LiteDatabase)field!.GetValue(_localDatabase)!;
    }
}
