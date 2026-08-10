using NSubstitute;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
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

    private static IAssetQueries CreateEmptyAssetQueries()
    {
        var queries = Substitute.For<IAssetQueries>();
        queries.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AssetDTO>>(new List<AssetDTO>()));
        return queries;
    }

    private static AssetDTO CreateAssetDto(
        string currencyCode,
        decimal currentValue,
        bool includeInNetWorth = true,
        DateTime? createdAt = null,
        DateOnly? dateSold = null)
    {
        return new AssetDTO
        {
            Id = AssetBuilder.AnAsset().Build().Id.Value,
            Name = "Test Asset",
            AssetTypeId = 1,
            AssetTypeName = "Stock",
            Icon = string.Empty,
            IncludeInNetWorth = includeInNetWorth,
            Visible = true,
            LastPriceUpdateAt = DateTime.UtcNow,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            DisplayOrder = 0,
            CurrentPrice = currentValue,
            CurrentValue = currentValue,
            CurrencyCode = currencyCode,
            IsSold = dateSold.HasValue,
            DateSold = dateSold,
            PreviousVisibility = true
        };
    }

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
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
        var allTimeHighReport = new AllTimeHighReport(clock, CreateEmptyAssetQueries());

        Assert.ThrowsAsync<ApplicationException>(() => allTimeHighReport.GetAsync(FiatCurrency.Brl, provider));
    }

    [Test]
    public async Task Should_Get_Incomplete_AllTimeHigh_For_FiatCurrency()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var allTimeHighReport = new AllTimeHighReport(clock, CreateEmptyAssetQueries());

        try
        {
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);
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
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var allTimeHighReport = new AllTimeHighReport(clock, CreateEmptyAssetQueries());

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

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);
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
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var allTimeHighReport = new AllTimeHighReport(clock, CreateEmptyAssetQueries());

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

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);
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

    [Test]
    public async Task Should_Include_Active_NetWorth_Asset_In_AllTimeHigh()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        var assetQueries = Substitute.For<IAssetQueries>();
        assetQueries.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AssetDTO>>(new List<AssetDTO>
        {
            CreateAssetDto(
                currencyCode: FiatCurrency.Usd.Code,
                currentValue: 1000m,
                includeInNetWorth: true,
                createdAt: new DateTime(2025, 1, 1))
        }));

        var allTimeHighReport = new AllTimeHighReport(clock, assetQueries);

        try
        {
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Currency, Is.EqualTo(FiatCurrency.Brl));
                // BRL account 1100 + USD asset 1000 converted to BRL at 5.5 = 5500
                Assert.That(result.Value.Value, Is.EqualTo(6600m));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 2, 1)));
                Assert.That(result.DaysUnderWater, Is.GreaterThan(0));
                Assert.That(result.HasAccountsWithoutTransactions, Is.True);
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Exclude_NonNetWorth_And_Sold_Assets_From_AllTimeHigh()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        var assetQueries = Substitute.For<IAssetQueries>();
        assetQueries.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AssetDTO>>(new List<AssetDTO>
        {
            CreateAssetDto(
                currencyCode: FiatCurrency.Usd.Code,
                currentValue: 1_000_000m,
                includeInNetWorth: false,
                createdAt: new DateTime(2025, 1, 1)),
            CreateAssetDto(
                currencyCode: FiatCurrency.Usd.Code,
                currentValue: 1_000_000m,
                includeInNetWorth: true,
                createdAt: new DateTime(2025, 1, 1),
                dateSold: new DateOnly(2025, 1, 15))
        }));

        var allTimeHighReport = new AllTimeHighReport(clock, assetQueries);

        try
        {
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);
            using (Assert.EnterMultipleScope())
            {
                // Only the BRL account transaction contributes; assets are excluded
                Assert.That(result.Value.Value, Is.EqualTo(1100m));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 2, 1)));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Calculate_DaysUnderWater_Based_On_Peak_Date()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var allTimeHighReport = new AllTimeHighReport(clock, CreateEmptyAssetQueries());

        try
        {
            // Peak occurs on the report end date: DaysUnderWater should be zero
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "End date deposit",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 12, 30),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var endDatePeakResult = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);

            _localDatabase.GetTransactions().DeleteAll();

            // Peak occurs earlier: DaysUnderWater should be positive
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Earlier deposit",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var earlierPeakResult = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(endDatePeakResult.DaysUnderWater, Is.EqualTo(0));
                Assert.That(earlierPeakResult.DaysUnderWater, Is.EqualTo(332));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Handle_Sats_Denominated_Asset()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));

        var assetQueries = Substitute.For<IAssetQueries>();
        assetQueries.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AssetDTO>>(new List<AssetDTO>
        {
            CreateAssetDto(
                currencyCode: "SATS",
                currentValue: 100_000_000m, // 1 BTC in satoshis
                includeInNetWorth: true,
                createdAt: new DateTime(2025, 1, 1))
        }));

        var allTimeHighReport = new AllTimeHighReport(clock, assetQueries);

        try
        {
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Test",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 100m, true)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);
            using (Assert.EnterMultipleScope())
            {
                // BRL account 1100 + SATS asset (1 BTC) converted to BRL at 5.5 = 550000
                Assert.That(result.Value.Value, Is.EqualTo(551100m));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 2, 1)));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }

    [Test]
    public async Task Should_Guard_NonPositive_Wealth()
    {
        var clock = new FakeClock(new DateTime(2025, 12, 31));
        var allTimeHighReport = new AllTimeHighReport(clock, CreateEmptyAssetQueries());

        try
        {
            // Zero out all seeded account balances so no positive wealth peak exists
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Zero BRL",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), 1000m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Zero USD",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_usdAccount.Id.ToString(), 1000m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Zero EUR",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new FiatDetails(_eurAccount.Id.ToString(), 1000m, false)
            }.Build());
            _localDatabase.GetTransactions().Insert(new TransactionBuilder()
            {
                Name = "Zero BTC",
                CategoryId = new CategoryId(),
                Date = new DateOnly(2025, 2, 1),
                TransactionDetails = new BitcoinDetails(_btcAccount.Id.ToString(), BtcValue.ParseBitcoin(1), false)
            }.Build());

            var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
            var result = await allTimeHighReport.GetAsync(FiatCurrency.Brl, provider);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Value.Value, Is.EqualTo(0m));
                Assert.That(result.Date, Is.EqualTo(new DateOnly(2025, 12, 30)));
                Assert.That(result.DaysUnderWater, Is.EqualTo(0));
                Assert.That(result.DeclineFromAth, Is.EqualTo(0m));
            }
        }
        finally
        {
            _localDatabase.GetTransactions().DeleteAll();
        }
    }
}
