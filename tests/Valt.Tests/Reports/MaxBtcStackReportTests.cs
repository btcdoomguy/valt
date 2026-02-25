using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MaxBtcStack;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class MaxBtcStackReportTests : DatabaseTest
{
    private AccountEntity _btcAccount1 = null!;
    private AccountEntity _btcAccount2 = null!;
    private AccountEntity _usdAccount = null!;

    protected override Task SeedDatabase()
    {
        _btcAccount1 = new BtcAccountBuilder()
        {
            Name = "BTC Account 1",
            Value = BtcValue.ParseSats(1_000_000) // 0.01 BTC initial
        }.Build();
        _localDatabase.GetAccounts().Insert(_btcAccount1);

        _btcAccount2 = new BtcAccountBuilder()
        {
            Name = "BTC Account 2",
            Value = BtcValue.ParseSats(500_000) // 0.005 BTC initial
        }.Build();
        _localDatabase.GetAccounts().Insert(_btcAccount2);

        _usdAccount = new FiatAccountBuilder()
        {
            Name = "USD Account",
            FiatCurrency = FiatCurrency.Usd,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_usdAccount);

        // Feed some fake BTC rates (needed for ReportDataProvider)
        var initialDate = new DateTime(2025, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity()
            {
                Date = currentDate,
                Price = 100000m
            });
            currentDate = currentDate.AddDays(1);
        }

        return base.SeedDatabase();
    }

    [Test]
    public void Should_Throw_Error_If_No_Transactions_Found()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var report = new MaxBtcStackReport(clock);

        Assert.ThrowsAsync<ApplicationException>(() => report.GetAsync(0, provider));
    }

    [Test]
    public async Task Should_Get_Max_Stack_For_Single_Account()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var report = new MaxBtcStackReport(clock);

        try
        {
            // Add 2 BTC on Feb 1 (initial 0.01 + 2 = 2.01 BTC = 201_000_000 sats)
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "BTC Income",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount1.Id.ToString(), BtcValue.ParseBitcoin(2), true)
            }.Build());

            // Spend 1 BTC on Mar 1 (2.01 - 1 = 1.01 BTC)
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "BTC Expense",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 3, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount1.Id.ToString(), BtcValue.ParseBitcoin(1), false)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var currentStack = 101_000_000L; // 1.01 BTC in sats
            var result = await report.GetAsync(currentStack, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.MaxStackInSats, Is.EqualTo(201_000_000L)); // 2.01 BTC peak
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 2, 1)));
                Assert.That(result.CurrentStackInSats, Is.EqualTo(currentStack));
                Assert.That(result.DeclineFromMaxPercent, Is.LessThan(0));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Sum_Multiple_Btc_Accounts()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var report = new MaxBtcStackReport(clock);

        try
        {
            // Add 1 BTC to account 1 on Feb 1
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "BTC Income 1",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount1.Id.ToString(), BtcValue.ParseBitcoin(1), true)
            }.Build());

            // Add 1 BTC to account 2 on Feb 1
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "BTC Income 2",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount2.Id.ToString(), BtcValue.ParseBitcoin(1), true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            // Combined: (1_000_000 + 100_000_000) + (500_000 + 100_000_000) = 201_500_000
            var currentStack = 201_500_000L;
            var result = await report.GetAsync(currentStack, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.MaxStackInSats, Is.EqualTo(201_500_000L));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 2, 1)));
                Assert.That(result.DeclineFromMaxPercent, Is.EqualTo(0m)); // At max
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Ignore_Fiat_Accounts()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var report = new MaxBtcStackReport(clock);

        try
        {
            // Add fiat transaction only
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Fiat Income",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 5000m, true)
            }.Build());

            // Add a small BTC transaction so we have at least one
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "BTC Income",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 3, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount1.Id.ToString(), BtcValue.ParseSats(100_000), true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var currentStack = 1_100_000L; // initial 1_000_000 + 100_000
            var result = await report.GetAsync(currentStack, provider);

            using (Assert.EnterMultipleScope())
            {
                // Max should only consider BTC: initial 1_000_000 + 100_000 = 1_100_000
                Assert.That(result.MaxStackInSats, Is.EqualTo(1_100_000L));
                Assert.That(result.DeclineFromMaxPercent, Is.EqualTo(0m));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Have_Zero_Decline_When_At_Max()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var report = new MaxBtcStackReport(clock);

        try
        {
            // Just add BTC, never spend
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "BTC Income",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount1.Id.ToString(), BtcValue.ParseBitcoin(1), true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var maxSats = 101_000_000L; // initial 1_000_000 + 100_000_000
            var result = await report.GetAsync(maxSats, provider);

            Assert.That(result.DeclineFromMaxPercent, Is.EqualTo(0m));
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }
}
