using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.SpendingEvolution.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
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

        PriceDataBuilder.SeedRange(_priceDatabase, new DateTime(2024, 1, 1), new DateTime(2025, 12, 31), 100000m,
            (FiatCurrency.Brl.Code, 5.5m));

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

    #endregion

    private async Task<Valt.App.Modules.SpendingEvolution.DTOs.SpendingEvolutionDataDto> ExecuteQuery(DateOnly from, DateOnly to)
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
            AccountIds = [_brlAccount.Id.ToString()],
            CategoryIds = []
        });
    }

    private TransactionEntity AddBrlExpense(DateOnly date, decimal amount, bool isExcluded = false)
    {
        var entity = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = date,
            Name = $"BRL Expense {date}",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, false),
            IsExcluded = isExcluded
        }.Build();
        _localDatabase.GetTransactions().Insert(entity);
        return entity;
    }
}
