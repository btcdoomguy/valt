using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Modules.Budget.Transactions.Services;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Transactions.Services;

public class TransactionAutoSatAmountCalculatorTests : DatabaseTest
{
    private AccountId _fromFiatAccountId = null!;
    private AccountId _toFiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        //init sample accounts and categories
        _fromFiatAccountId = IdGenerator.Generate();
        _toFiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fiatAccount1 = new FiatAccountBuilder()
        {
            Id = _fromFiatAccountId,
            Name = "Fiat Account1",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 100000m
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount1);

        var fiatAccount2 = new FiatAccountBuilder()
        {
            Id = _toFiatAccountId,
            Name = "Fiat Account2",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 100000m
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount2);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Test")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);

        //init sample bitcoin data history
        var bitcoinDataEntity1 = new BitcoinDataEntity()
        {
            Date = new DateTime(2023, 1, 1),
            Price = 50000m
        };
        var bitcoinDataEntity2 = new BitcoinDataEntity()
        {
            Date = new DateTime(2023, 1, 2),
            Price = 51000m
        };
        var bitcoinDataEntity3 = new BitcoinDataEntity()
        {
            Date = new DateTime(2023, 1, 3),
            Price = 52000m
        };

        _priceDatabase.GetBitcoinData().Insert(bitcoinDataEntity1);
        _priceDatabase.GetBitcoinData().Insert(bitcoinDataEntity2);
        _priceDatabase.GetBitcoinData().Insert(bitcoinDataEntity3);

        //init sample fiat data history
        var fiatDataEntity1 = new FiatDataEntity()
        {
            Date = new DateTime(2023, 1, 1),
            Currency = FiatCurrency.Brl.Code,
            Price = 5m
        };
        var fiatDataEntity2 = new FiatDataEntity()
        {
            Date = new DateTime(2023, 1, 2),
            Currency = FiatCurrency.Brl.Code,
            Price = 5.1m
        };
        var fiatDataEntity3 = new FiatDataEntity()
        {
            Date = new DateTime(2023, 1, 3),
            Currency = FiatCurrency.Brl.Code,
            Price = 5.2m
        };

        _priceDatabase.GetFiatData().Insert(fiatDataEntity1);
        _priceDatabase.GetFiatData().Insert(fiatDataEntity2);
        _priceDatabase.GetFiatData().Insert(fiatDataEntity3);
    }

    [Test]
    [TestCaseSource(nameof(Should_Update_Transaction_AutoSatAmount_Cases))]
    public async Task Should_Update_Transaction_AutoSatAmount(DateOnly transactionDate, FiatValue transactionValue,
        BtcValue expectedSats)
    {
        //creates a valid transaction to calculate the autosatamount rate
        var transaction = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = transactionDate,
            Name = "My Transaction",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails =
                new FiatToFiatDetails(_fromFiatAccountId, _toFiatAccountId, transactionValue, transactionValue)
        }.BuildDomainObject();

        await _transactionRepository.SaveTransactionAsync(transaction);

        var localHistoryProvider = new LivePricesUpdaterJob(_priceDatabase);

        var service = new TransactionAutoSatAmountCalculator(_transactionRepository, _localDatabase, localHistoryProvider,
            NullLogger<TransactionAutoSatAmountCalculator>.Instance);
        await service.UpdateAutoSatAmountAsync(transaction.Id);

        //reads the db and checks if the autosatamount was properly set
        var transactionEntity = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(transactionEntity, Is.Not.Null);
        Assert.That(transactionEntity.AutoSatAmountDetails, Is.Not.Null);
        Assert.That(transactionEntity.AutoSatAmountDetails.SatAmountState, Is.EqualTo(SatAmountState.Processed));
        Assert.That(transactionEntity.AutoSatAmountDetails.IsAutoSatAmount, Is.True);
        Assert.That(transactionEntity.AutoSatAmountDetails.SatAmount, Is.EqualTo(expectedSats));
    }

    private static readonly IEnumerable<TestCaseData> Should_Update_Transaction_AutoSatAmount_Cases = new[]
    {
        new TestCaseData(new DateOnly(2023, 1, 1), FiatValue.New(1000m), BtcValue.ParseSats(400000)),
        new TestCaseData(new DateOnly(2023, 1, 2), FiatValue.New(1000m), BtcValue.ParseSats(384468)),
    };

    [Test]
    public async Task Should_Ignore_Transaction_If_Outside_MaxRange()
    {
        //creates a valid transaction to calculate the autosatamount rate
        var transaction = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 5), //date outside range
            Name = "My Transaction",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatToFiatDetails(_fromFiatAccountId, _toFiatAccountId, FiatValue.New(1000m),
                FiatValue.New(1000m))
        }.BuildDomainObject();

        await _transactionRepository.SaveTransactionAsync(transaction);

        var localHistoryProvider = new LivePricesUpdaterJob(_priceDatabase);

        var service = new TransactionAutoSatAmountCalculator(_transactionRepository, _localDatabase, localHistoryProvider,
            NullLogger<TransactionAutoSatAmountCalculator>.Instance);
        await service.UpdateAutoSatAmountAsync(transaction.Id);

        //reads the db and checks if the autosatamount was properly set
        var transactionEntity = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(transactionEntity, Is.Not.Null);
        Assert.That(transactionEntity.AutoSatAmountDetails, Is.Not.Null);
        Assert.That(transactionEntity.AutoSatAmountDetails.SatAmountState, Is.EqualTo(SatAmountState.Pending));
    }

    [Test]
    public async Task Should_Mark_Error_In_Transaction_If_Rates_AreZero()
    {
        //adds rates without value
        var bitcoinDataEntity1 = new BitcoinDataEntity()
        {
            Date = new DateTime(2022, 12, 31),
            Price = 0m
        };
        _priceDatabase.GetBitcoinData().Insert(bitcoinDataEntity1);

        var fiatDataEntity1 = new FiatDataEntity()
        {
            Date = new DateTime(2022, 12, 31),
            Currency = FiatCurrency.Brl.Code,
            Price = 0m
        };

        _priceDatabase.GetFiatData().Insert(fiatDataEntity1);

        //creates a valid transaction to calculate the autosatamount rate
        var transaction = new TransactionBuilder()
        {
            Id = IdGenerator.Generate(),
            CategoryId = _categoryId,
            Date = new DateOnly(2022, 12, 31), //day without entries in the db
            Name = "My Transaction",
            AutoSatAmountDetails = AutoSatAmountDetails.Pending,
            TransactionDetails = new FiatToFiatDetails(_fromFiatAccountId, _toFiatAccountId, FiatValue.New(1000m),
                FiatValue.New(1000m))
        }.BuildDomainObject();

        await _transactionRepository.SaveTransactionAsync(transaction);

        var localHistoryProvider = new LivePricesUpdaterJob(_priceDatabase);

        var service = new TransactionAutoSatAmountCalculator(_transactionRepository, _localDatabase, localHistoryProvider,
            NullLogger<TransactionAutoSatAmountCalculator>.Instance);
        await service.UpdateAutoSatAmountAsync(transaction.Id);

        //reads the db and checks if the autosatamount was properly set
        var transactionEntity = await _transactionRepository.GetTransactionByIdAsync(transaction.Id);
        Assert.That(transactionEntity, Is.Not.Null);
        Assert.That(transactionEntity.AutoSatAmountDetails, Is.Not.Null);
        Assert.That(transactionEntity.AutoSatAmountDetails.SatAmountState, Is.EqualTo(SatAmountState.Missing));
    }
}