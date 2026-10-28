using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.IncomeByCategory;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class IncomeByCategoryReportTests : DatabaseTest
{
    private AccountEntity _usdAccount = null!;
    private CategoryId _salaryCategoryId = null!;
    private CategoryId _freelanceCategoryId = null!;

    protected override Task SeedDatabase()
    {
        _salaryCategoryId = IdGenerator.Generate();
        _freelanceCategoryId = IdGenerator.Generate();

        _localDatabase.GetCategories().Insert(new CategoryBuilder()
            .WithId(_salaryCategoryId)
            .WithName("Salary")
            .WithIcon(Icon.Empty)
            .Build());

        _localDatabase.GetCategories().Insert(new CategoryBuilder()
            .WithId(_freelanceCategoryId)
            .WithName("Freelance")
            .WithIcon(Icon.Empty)
            .Build());

        _usdAccount = new FiatAccountBuilder()
        {
            Name = "USD Account",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        // Seed price data for the test date range
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
                Price = 5m
            });

            currentDate = currentDate.AddDays(1);
        }

        // January 2025: unflagged salary income
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _salaryCategoryId,
            Date = new DateOnly(2025, 01, 05),
            Name = "January Salary",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 2000m, true)
        }.Build());

        return base.SeedDatabase();
    }

    [Test]
    public async Task Should_Calculate_Income_By_Category()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new IncomeByCategoryReport(new NullLogger<IncomeByCategoryReport>());

        var filter = new IIncomeByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()) },
            new[] { _salaryCategoryId, _freelanceCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31)),
            FiatCurrency.Usd,
            filter,
            provider);

        Assert.That(result.MainCurrency, Is.EqualTo(FiatCurrency.Usd));
        var salaryItem = result.Items.Single(x => x.CategoryId == _salaryCategoryId);
        Assert.That(salaryItem.CategoryName, Is.EqualTo("Salary"));
        Assert.That(salaryItem.FiatTotal, Is.EqualTo(2000m));
    }

    #region Exclusion Flag Tests

    [Test]
    public async Task Flagged_Income_Is_Excluded_From_Category_Total()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var report = new IncomeByCategoryReport(new NullLogger<IncomeByCategoryReport>());
        var range = new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31));

        var filter = new IIncomeByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()) },
            new[] { _freelanceCategoryId }
        );

        var before = await report.GetAsync(baseDate, range, FiatCurrency.Usd, filter,
            new ReportDataProvider(_priceDatabase, _localDatabase, clock));

        var entity = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _freelanceCategoryId,
            Date = new DateOnly(2025, 01, 15),
            Name = "Flagged Freelance Income",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 3000m, true),
            IsExcluded = true
        }.Build();
        _localDatabase.GetTransactions().Insert(entity);

        try
        {
            var after = await report.GetAsync(baseDate, range, FiatCurrency.Usd, filter,
                new ReportDataProvider(_priceDatabase, _localDatabase, clock));

            // The flagged income must not create nor change the Freelance bucket
            Assert.That(after.Items, Is.EqualTo(before.Items));
        }
        finally
        {
            _localDatabase.GetTransactions().Delete(entity.Id);
        }
    }

    [Test]
    public async Task Unflagged_Income_Still_Aggregates_Normally()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var report = new IncomeByCategoryReport(new NullLogger<IncomeByCategoryReport>());
        var range = new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31));

        var filter = new IIncomeByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()) },
            new[] { _freelanceCategoryId }
        );

        var before = await report.GetAsync(baseDate, range, FiatCurrency.Usd, filter,
            new ReportDataProvider(_priceDatabase, _localDatabase, clock));

        var entity = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _freelanceCategoryId,
            Date = new DateOnly(2025, 01, 15),
            Name = "Unflagged Freelance Income",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 3000m, true),
            IsExcluded = false
        }.Build();
        _localDatabase.GetTransactions().Insert(entity);

        try
        {
            var after = await report.GetAsync(baseDate, range, FiatCurrency.Usd, filter,
                new ReportDataProvider(_priceDatabase, _localDatabase, clock));

            var freelanceBefore = before.Items.FirstOrDefault(x => x.CategoryId == _freelanceCategoryId)?.FiatTotal ?? 0m;
            Assert.That(after.Items.Single(x => x.CategoryId == _freelanceCategoryId).FiatTotal,
                Is.EqualTo(freelanceBefore + 3000m));
        }
        finally
        {
            _localDatabase.GetTransactions().Delete(entity.Id);
        }
    }

    #endregion
}
