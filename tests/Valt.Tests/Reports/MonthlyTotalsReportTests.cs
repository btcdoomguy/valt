using ExCSS;
using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class MonthlyTotalsReportTests : DatabaseTest
{
    private AccountEntity _btcAccount = null!;
    private AccountEntity _usdAccount = null!;
    private AccountEntity _brlAccount = null!;
    private AccountEntity _eurAccount = null!;

    private CategoryId _categoryId = null!;

    protected override Task SeedDatabase()
    {
        _categoryId = IdGenerator.Generate();

        //initialize demo accounts
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
            Value = FiatValue.New(1000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        _brlAccount = new FiatAccountBuilder()
        {
            Name = "BRL Account",
            FiatCurrency = FiatCurrency.Brl,
            Value = FiatValue.New(1000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_brlAccount);

        _eurAccount = new FiatAccountBuilder()
        {
            Name = "EUR Account",
            FiatCurrency = FiatCurrency.Eur,
            Value = FiatValue.New(1000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_eurAccount);

        //feed some fake rates and predefined transactions

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
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity()
            {
                Date = currentDate,
                Currency = FiatCurrency.Eur.Code,
                Price = 0.75m
            });

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "USD Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new FiatDetails(_usdAccount.Id.ToString(), 100m, true)
                    : new FiatDetails(_usdAccount.Id.ToString(), 1m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "BRL Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
                    : new FiatDetails(_brlAccount.Id.ToString(), 1m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "EUR Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new FiatDetails(_eurAccount.Id.ToString(), 100m, true)
                    : new FiatDetails(_eurAccount.Id.ToString(), 1m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Id = IdGenerator.Generate(),
                CategoryId = _categoryId,
                Date = DateOnly.FromDateTime(currentDate),
                Name = "BTC Transaction",
                AutoSatAmountDetails = AutoSatAmountDetails.Pending,
                TransactionDetails = currentDate.Day == 1
                    ? new BitcoinDetails(_btcAccount.Id.ToString(), 10000, true)
                    : new BitcoinDetails(_btcAccount.Id.ToString(), 100, false)
            }.Build());

            currentDate = currentDate.AddDays(1);
        }

        //some transfers
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = DateOnly.FromDateTime(new DateTime(2024, 12, 01)),
            Name = "BTC to USD Transfer",
            TransactionDetails = new BitcoinToFiatDetails(_btcAccount.Id.ToString(), _usdAccount.Id.ToString(),
                BtcValue.ParseSats(10000), FiatValue.New(1000m))
        }.Build());
        _localDatabase.GetTransactions().Insert(new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = DateOnly.FromDateTime(new DateTime(2025, 1, 01)),
            Name = "USD to BTC Transfer",
            TransactionDetails = new FiatToBitcoinDetails(_usdAccount.Id.ToString(), _btcAccount.Id.ToString(),
                FiatValue.New(1000m), BtcValue.ParseSats(10000))
        }.Build());

        return base.SeedDatabase();
    }

    [Test]
    public async Task Should_Calculate_MonthlyTotals()
    {
        var baseDate = new DateOnly(2025, 12, 31);
        var clock = new FakeClock(baseDate.ToDateTime(TimeOnly.MinValue));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());

        var result2024 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2024, 01, 01), new DateOnly(2024, 12, 31)),
            FiatCurrency.Brl, provider);
        var result2025 = await report.GetAsync(baseDate, new DateOnlyRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 12, 31)),
            FiatCurrency.Brl, provider);

        Assert.That(result2024.MainCurrency, Is.EqualTo(FiatCurrency.Brl));
        Assert.That(result2024.Items[0].BtcTotal, Is.EqualTo(1.00007m));
        Assert.That(result2024.Items[0].FiatTotal, Is.EqualTo(564840.17m));
        Assert.That(result2024.Items[0].BtcMonthlyChange, Is.Zero);
        Assert.That(result2024.Items[0].BtcYearlyChange, Is.Zero);
        Assert.That(result2024.Items[0].Income, Is.EqualTo(1383.33m));
        Assert.That(result2024.Items[0].Expenses, Is.EqualTo(-415m));

        Assert.That(result2024.Items[11].BtcTotal, Is.EqualTo(1.000746m));
        Assert.That(result2024.Items[11].FiatTotal, Is.EqualTo(581446.63m));
        Assert.That(result2024.Items[11].BtcMonthlyChange, Is.EqualTo(0.00m));
        Assert.That(result2024.Items[11].BtcYearlyChange, Is.Zero);
        Assert.That(result2024.Items[11].Income, Is.EqualTo(1383.33m));
        Assert.That(result2024.Items[11].Expenses, Is.EqualTo(-415m));

        Assert.That(result2025.Items[0].BtcTotal, Is.EqualTo(1.000916m));
        Assert.That(result2025.Items[0].FiatTotal, Is.EqualTo(577008.47m));
        Assert.That(result2025.Items[0].BtcMonthlyChange, Is.EqualTo(0.02m));
        Assert.That(result2025.Items[0].BtcYearlyChange, Is.EqualTo(0.02m));
        Assert.That(result2025.Items[0].Income, Is.EqualTo(1383.33m));
        Assert.That(result2025.Items[0].Expenses, Is.EqualTo(-415m));

        Assert.That(result2025.Items[11].BtcTotal, Is.EqualTo(1.001693m));
        Assert.That(result2025.Items[11].FiatTotal, Is.EqualTo(588184.32m));
        Assert.That(result2025.Items[11].BtcMonthlyChange, Is.EqualTo(0.01m));
        Assert.That(result2025.Items[11].BtcYearlyChange, Is.EqualTo(0.09m));
        Assert.That(result2025.Items[11].Income, Is.EqualTo(1383.33m));
        Assert.That(result2025.Items[11].Expenses, Is.EqualTo(-415m));
    }
}
