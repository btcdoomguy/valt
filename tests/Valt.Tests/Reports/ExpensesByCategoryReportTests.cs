using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class ExpensesByCategoryReportTests : DatabaseTest
{
    private AccountEntity _btcAccount = null!;
    private AccountEntity _usdAccount = null!;
    private AccountEntity _brlAccount = null!;

    private CategoryId _foodCategoryId = null!;
    private CategoryId _transportCategoryId = null!;
    private CategoryId _utilitiesCategoryId = null!;

    protected override Task SeedDatabase()
    {
        _foodCategoryId = IdGenerator.Generate();
        _transportCategoryId = IdGenerator.Generate();
        _utilitiesCategoryId = IdGenerator.Generate();

        // Initialize categories
        _localDatabase.GetCategories().Insert(new CategoryBuilder()
            .WithId(_foodCategoryId)
            .WithName("Food")
            .WithIcon(Icon.Empty)
            .Build());

        _localDatabase.GetCategories().Insert(new CategoryBuilder()
            .WithId(_transportCategoryId)
            .WithName("Transport")
            .WithIcon(Icon.Empty)
            .Build());

        _localDatabase.GetCategories().Insert(new CategoryBuilder()
            .WithId(_utilitiesCategoryId)
            .WithName("Utilities")
            .WithIcon(Icon.Empty)
            .Build());

        // Initialize accounts
        _btcAccount = new BtcAccountBuilder()
        {
            Name = "BTC Account",
            Value = BtcValue.ParseBitcoin(1)
        }.Build();
        _localDatabase.GetAccounts().Insert(_btcAccount);

        _usdAccount = new FiatAccountBuilder()
        {
            Name = "USD Account",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        _brlAccount = new FiatAccountBuilder()
        {
            Name = "BRL Account",
            FiatCurrency = FiatCurrency.Brl,
            Value = FiatValue.New(50000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_brlAccount);

        // Seed price data for the test date range
        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity()
            {
                Date = currentDate,
                Price = 100000m // $100k per BTC
            });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity()
            {
                Date = currentDate,
                Currency = FiatCurrency.Brl.Code,
                Price = 5m // 1 USD = 5 BRL
            });

            currentDate = currentDate.AddDays(1);
        }

        // Create spending transactions (Credit=false = expense/debt)
        // January 2025: Food expenses in USD
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _foodCategoryId,
            Date = new DateOnly(2025, 01, 05),
            Name = "Grocery Shopping",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 100m, false) // spending $100
        }.Build());

        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _foodCategoryId,
            Date = new DateOnly(2025, 01, 15),
            Name = "Restaurant",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 50m, false) // spending $50
        }.Build());

        // January 2025: Transport expenses in BRL
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _transportCategoryId,
            Date = new DateOnly(2025, 01, 10),
            Name = "Gas",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 250m, false) // spending 250 BRL = $50
        }.Build());

        // January 2025: Utilities expense in BTC
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _utilitiesCategoryId,
            Date = new DateOnly(2025, 01, 20),
            Name = "Internet Bill (BTC)",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new BitcoinDetails(_btcAccount.Id.ToString(), 10000, false) // spending 10000 sats = $10
        }.Build());

        // Income transactions (Credit=true - should be ignored by report)
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _foodCategoryId,
            Date = new DateOnly(2025, 01, 25),
            Name = "Refund",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 20m, true) // receiving $20 (income)
        }.Build());

        // February 2025: More expenses
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _foodCategoryId,
            Date = new DateOnly(2025, 02, 10),
            Name = "Supermarket",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 200m, false)
        }.Build());

        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _transportCategoryId,
            Date = new DateOnly(2025, 02, 15),
            Name = "Uber",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, false) // 100 BRL = $20
        }.Build());

        return base.SeedDatabase();
    }

    [Test]
    public async Task Should_Calculate_Expenses_By_Category_In_USD()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()), new AccountId(_brlAccount.Id.ToString()), new AccountId(_btcAccount.Id.ToString()) },
            new[] { _foodCategoryId, _transportCategoryId, _utilitiesCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31)),
            FiatCurrency.Usd,
            filter,
            provider);

        Assert.That(result.MainCurrency, Is.EqualTo(FiatCurrency.Usd));
        Assert.That(result.Items.Count, Is.EqualTo(3));

        // Food: $100 + $50 = $150 (the $20 income should be ignored)
        var foodItem = result.Items.Single(x => x.CategoryId == _foodCategoryId);
        Assert.That(foodItem.CategoryName, Is.EqualTo("Food"));
        Assert.That(foodItem.FiatTotal, Is.EqualTo(150m));

        // Transport: 250 BRL / 5 (rate) = $50
        var transportItem = result.Items.Single(x => x.CategoryId == _transportCategoryId);
        Assert.That(transportItem.CategoryName, Is.EqualTo("Transport"));
        Assert.That(transportItem.FiatTotal, Is.EqualTo(50m));

        // Utilities: 10000 sats = 0.0001 BTC * $100000 = $10
        var utilitiesItem = result.Items.Single(x => x.CategoryId == _utilitiesCategoryId);
        Assert.That(utilitiesItem.CategoryName, Is.EqualTo("Utilities"));
        Assert.That(utilitiesItem.FiatTotal, Is.EqualTo(10m));
    }

    [Test]
    public async Task Should_Calculate_Expenses_By_Category_In_BRL()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()), new AccountId(_brlAccount.Id.ToString()), new AccountId(_btcAccount.Id.ToString()) },
            new[] { _foodCategoryId, _transportCategoryId, _utilitiesCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31)),
            FiatCurrency.Brl,
            filter,
            provider);

        Assert.That(result.MainCurrency, Is.EqualTo(FiatCurrency.Brl));

        // Food: $150 USD * 5 (BRL rate) = 750 BRL
        var foodItem = result.Items.Single(x => x.CategoryId == _foodCategoryId);
        Assert.That(foodItem.FiatTotal, Is.EqualTo(750m));

        // Transport: 250 BRL (account currency matches target, kept as-is)
        var transportItem = result.Items.Single(x => x.CategoryId == _transportCategoryId);
        Assert.That(transportItem.FiatTotal, Is.EqualTo(250m));

        // Utilities (BTC): 10000 sats = 0.0001 BTC * $100000 * 5 (BRL rate) = 50 BRL
        var utilitiesItem = result.Items.Single(x => x.CategoryId == _utilitiesCategoryId);
        Assert.That(utilitiesItem.FiatTotal, Is.EqualTo(50m));
    }

    [Test]
    public async Task Should_Filter_By_Account()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        // Only include USD account
        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()) },
            new[] { _foodCategoryId, _transportCategoryId, _utilitiesCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31)),
            FiatCurrency.Usd,
            filter,
            provider);

        // Should only have Food category (USD account transactions)
        Assert.That(result.Items.Count, Is.EqualTo(1));
        var foodItem = result.Items.Single(x => x.CategoryId == _foodCategoryId);
        Assert.That(foodItem.FiatTotal, Is.EqualTo(150m));
    }

    [Test]
    public async Task Should_Filter_By_Category()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        // Only include Food category
        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()), new AccountId(_brlAccount.Id.ToString()), new AccountId(_btcAccount.Id.ToString()) },
            new[] { _foodCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31)),
            FiatCurrency.Usd,
            filter,
            provider);

        // Should only have Food category
        Assert.That(result.Items.Count, Is.EqualTo(1));
        var foodItem = result.Items.Single(x => x.CategoryId == _foodCategoryId);
        Assert.That(foodItem.FiatTotal, Is.EqualTo(150m));
    }

    [Test]
    public async Task Should_Calculate_Expenses_Across_Multiple_Months()
    {
        var baseDate = new DateOnly(2025, 02, 28);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()), new AccountId(_brlAccount.Id.ToString()), new AccountId(_btcAccount.Id.ToString()) },
            new[] { _foodCategoryId, _transportCategoryId, _utilitiesCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 02, 28)),
            FiatCurrency.Usd,
            filter,
            provider);

        // Food: $150 (Jan) + $200 (Feb) = $350
        var foodItem = result.Items.Single(x => x.CategoryId == _foodCategoryId);
        Assert.That(foodItem.FiatTotal, Is.EqualTo(350m));

        // Transport: $50 (Jan) + $20 (Feb from 100 BRL) = $70
        var transportItem = result.Items.Single(x => x.CategoryId == _transportCategoryId);
        Assert.That(transportItem.FiatTotal, Is.EqualTo(70m));

        // Utilities: $10 (Jan only)
        var utilitiesItem = result.Items.Single(x => x.CategoryId == _utilitiesCategoryId);
        Assert.That(utilitiesItem.FiatTotal, Is.EqualTo(10m));
    }

    [Test]
    public async Task Should_Return_Empty_When_No_Transactions_In_Range()
    {
        var baseDate = new DateOnly(2023, 01, 31); // Before any transactions
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()) },
            new[] { _foodCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2023, 01, 01), new DateOnly(2023, 01, 31)),
            FiatCurrency.Usd,
            filter,
            provider);

        // Should return empty result when no transactions match the date range
        Assert.That(result.Items, Is.Empty);
        Assert.That(result.MainCurrency, Is.EqualTo(FiatCurrency.Usd));
    }

    [Test]
    public async Task Should_Only_Include_Expenses_Not_Income()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()) },
            new[] { _foodCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31)),
            FiatCurrency.Usd,
            filter,
            provider);

        // Food: $100 + $50 = $150 (the $20 refund/income is NOT included)
        var foodItem = result.Items.Single(x => x.CategoryId == _foodCategoryId);
        Assert.That(foodItem.FiatTotal, Is.EqualTo(150m));
    }

    [Test]
    public async Task Should_Order_Results_By_Category_Name()
    {
        var baseDate = new DateOnly(2025, 01, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new ExpensesByCategoryReport(new NullLogger<ExpensesByCategoryReport>());

        var filter = new IExpensesByCategoryReport.Filter(
            new[] { new AccountId(_usdAccount.Id.ToString()), new AccountId(_brlAccount.Id.ToString()), new AccountId(_btcAccount.Id.ToString()) },
            new[] { _foodCategoryId, _transportCategoryId, _utilitiesCategoryId }
        );

        var result = await report.GetAsync(
            baseDate,
            new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 01, 31)),
            FiatCurrency.Usd,
            filter,
            provider);

        // Categories should be ordered alphabetically: Food, Transport, Utilities
        Assert.That(result.Items[0].CategoryName, Is.EqualTo("Food"));
        Assert.That(result.Items[1].CategoryName, Is.EqualTo("Transport"));
        Assert.That(result.Items[2].CategoryName, Is.EqualTo("Utilities"));
    }
}
