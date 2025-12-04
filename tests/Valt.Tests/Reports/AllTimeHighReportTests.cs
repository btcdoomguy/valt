using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class AllTimeHighReportTests : DatabaseTest
{
    private AccountEntity _btcAccount = null!;
    private AccountEntity _usdAccount = null!;
    private AccountEntity _brlAccount = null!;
    private AccountEntity _eurAccount = null!;

    protected override Task SeedDatabase()
    {
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

        //feed some fake rates

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

            currentDate = currentDate.AddDays(1);
        }

        return base.SeedDatabase();
    }

    [Test]
    public void Should_Throw_Error_If_No_Transactions_Found()
    {
        var allTimeHighReport = new AllTimeHighReport(_priceDatabase, _localDatabase, new FakeClock(new DateTime(2025, 12, 31)));

        Assert.ThrowsAsync<ApplicationException>(() => allTimeHighReport.GetAsync(FiatCurrency.Brl));
    }

    [Test]
    public async Task Should_Get_Incomplete_AllTimeHigh_For_FiatCurrency()
    {
        var allTimeHighReport = new AllTimeHighReport(_priceDatabase, _localDatabase, new FakeClock(new DateTime(2025, 12, 31)));

        try
        {
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Currency, Is.EqualTo(FiatCurrency.Brl));
                Assert.That(result.Value.Value, Is.EqualTo(1100m));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 2, 1)));
                Assert.That(result.HasAccountsWithoutTransactions, Is.True);
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Get_Complete_AllTimeHigh_For_FiatCurrency()
    {
        var allTimeHighReport = new AllTimeHighReport(_priceDatabase, _localDatabase, new FakeClock(new DateTime(2025, 12, 31)));

        try
        {
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 3, 1),
                TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 100m, true)
            }.Build());

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 4, 1),
                TransactionDetails = new FiatDetails(_eurAccount.Id.ToString(), 100m, true)
            }.Build());

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 5, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount.Id.ToString(), BtcValue.ParseBitcoin(1), true)
            }.Build());

            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Currency, Is.EqualTo(FiatCurrency.Brl));
                Assert.That(result.Value.Value, Is.EqualTo(1115216.67m));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 5, 1)));
                Assert.That(result.HasAccountsWithoutTransactions, Is.False);
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }
    
    [Test]
    public async Task Should_Properly_Calculate_AllTimeHigh_For_FiatCurrency_After_Rate_Change()
    {
        var allTimeHighReport = new AllTimeHighReport(_priceDatabase, _localDatabase, new FakeClock(new DateTime(2025, 12, 31)));

        try
        {
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 3, 1),
                TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 100m, true)
            }.Build());

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 4, 1),
                TransactionDetails = new FiatDetails(_eurAccount.Id.ToString(), 100m, true)
            }.Build());

            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 5, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount.Id.ToString(), BtcValue.ParseBitcoin(1), true)
            }.Build());
            
            //replace one of the btc rates to a higher one
            var dateToReplace = _priceDatabase.GetBitcoinData().FindOne(x => x.Date == new DateTime(2025, 6, 1));
            _priceDatabase.GetBitcoinData().Delete(dateToReplace.Id);
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity()
            {
                Date = new DateTime(2025, 6, 1),
                Price = 200000m
            });

            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Currency, Is.EqualTo(FiatCurrency.Brl));
                Assert.That(result.Value.Value, Is.EqualTo(2215216.67m));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 6, 1)));
                Assert.That(result.HasAccountsWithoutTransactions, Is.False);
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }
}