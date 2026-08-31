using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Valt.Core.Common;
using Valt.Infra;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Tests.Jobs;

[TestFixture]
public class LivePricesUpdaterJobTests : IntegrationTest
{
    protected override Task SeedDatabase()
    {
        // Create fiat accounts so GetAvailableFiatCurrencies() returns USD and BRL
        _localDatabase.GetAccounts().Insert(new AccountEntity
        {
            Id = ObjectId.NewObjectId(),
            Name = "Test USD Account",
            Currency = FiatCurrency.Usd.Code,
            AccountEntityType = AccountEntityType.Fiat,
            InitialAmount = 1000m
        });

        _localDatabase.GetAccounts().Insert(new AccountEntity
        {
            Id = ObjectId.NewObjectId(),
            Name = "Test BRL Account",
            Currency = FiatCurrency.Brl.Code,
            AccountEntityType = AccountEntityType.Fiat,
            InitialAmount = 5000m
        });

        // Seed price data
        _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity
        {
            Date = new DateOnly(2023, 1, 1).ToValtDateTime(),
            Price = 10000m
        });

        _priceDatabase.GetFiatData().Insert(new FiatDataEntity
        {
            Currency = FiatCurrency.Usd.Code,
            Date = new DateOnly(2023, 1, 1).ToValtDateTime(),
            Price = 1m
        });

        _priceDatabase.GetFiatData().Insert(new FiatDataEntity
        {
            Currency = FiatCurrency.Brl.Code,
            Date = new DateOnly(2023, 1, 1).ToValtDateTime(),
            Price = 5m
        });

        return Task.CompletedTask;
    }

    [Test]
    public async Task Should_Publish_Stored_Rates_When_Live_Api_Fails()
    {
        LivePriceUpdateMessage? receivedValue = null;

        WeakReferenceMessenger.Default.Register<LivePriceUpdateMessage>(
            this,
            (recipient, message) => { receivedValue = message; });

        var failingFiatProviderSelector = Substitute.For<IFiatPriceProviderSelector>();
        failingFiatProviderSelector.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns<Task<FiatUsdPrice>>(_ => throw new HttpRequestException("No internet"));

        var failingBtcProvider = Substitute.For<IBitcoinPriceProvider>();
        failingBtcProvider.GetAsync().Returns<Task<BtcPrice>>(_ => throw new TimeoutException("No internet"));

        ReplaceService(failingFiatProviderSelector);
        ReplaceService(failingBtcProvider);

        var job = _serviceProvider.GetRequiredService<LivePricesUpdaterJob>();

        await job.RunAsync(CancellationToken.None);

        WeakReferenceMessenger.Default.Unregister<LivePriceUpdateMessage>(this);

        Assert.That(receivedValue, Is.Not.Null);
        Assert.That(receivedValue!.IsUpToDate, Is.False);
        Assert.That(receivedValue.Btc.Items, Has.Some.Matches<BtcPrice.Item>(x => x.CurrencyCode == FiatCurrency.Usd.Code));
        Assert.That(receivedValue.Fiat.Items, Has.Some.Matches<FiatUsdPrice.Item>(x => x.Currency.Code == FiatCurrency.Usd.Code));
        Assert.That(receivedValue.Fiat.Items, Has.Some.Matches<FiatUsdPrice.Item>(x => x.Currency.Code == FiatCurrency.Brl.Code));
    }

    [Test]
    public async Task Should_Merge_Live_Rates_With_Stored_Rates_When_Currency_Missing()
    {
        LivePriceUpdateMessage? receivedValue = null;

        WeakReferenceMessenger.Default.Register<LivePriceUpdateMessage>(
            this,
            (recipient, message) => { receivedValue = message; });

        var partialFiat = new FiatUsdPrice(DateTime.UtcNow, true,
            [new FiatUsdPrice.Item(FiatCurrency.Usd, 1m)]);

        var liveBtc = new BtcPrice(DateTime.UtcNow, true,
            [new BtcPrice.Item(FiatCurrency.Usd.Code, 20000m, 10000m)]);

        var fiatSelector = Substitute.For<IFiatPriceProviderSelector>();
        fiatSelector.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>()).Returns(partialFiat);

        var btcProvider = Substitute.For<IBitcoinPriceProvider>();
        btcProvider.GetAsync().Returns(liveBtc);

        ReplaceService(fiatSelector);
        ReplaceService(btcProvider);

        var job = _serviceProvider.GetRequiredService<LivePricesUpdaterJob>();

        await job.RunAsync(CancellationToken.None);

        WeakReferenceMessenger.Default.Unregister<LivePriceUpdateMessage>(this);

        Assert.That(receivedValue, Is.Not.Null);
        Assert.That(receivedValue!.IsUpToDate, Is.False);
        Assert.That(receivedValue.Fiat.Items, Has.Some.Matches<FiatUsdPrice.Item>(x => x.Currency.Code == FiatCurrency.Usd.Code));
        Assert.That(receivedValue.Fiat.Items, Has.Some.Matches<FiatUsdPrice.Item>(x => x.Currency.Code == FiatCurrency.Brl.Code));
        Assert.That(receivedValue.Btc.Items, Has.Some.Matches<BtcPrice.Item>(x => x.CurrencyCode == FiatCurrency.Usd.Code && x.Price == 20000m));
    }

    [Test]
    public async Task Should_Mark_UpToDate_When_All_Configured_Currencies_Are_Returned()
    {
        LivePriceUpdateMessage? receivedValue = null;

        WeakReferenceMessenger.Default.Register<LivePriceUpdateMessage>(
            this,
            (recipient, message) => { receivedValue = message; });

        var fullFiat = new FiatUsdPrice(DateTime.UtcNow, true,
        [
            new FiatUsdPrice.Item(FiatCurrency.Usd, 1m),
            new FiatUsdPrice.Item(FiatCurrency.Brl, 5.2m)
        ]);

        var liveBtc = new BtcPrice(DateTime.UtcNow, true,
            [new BtcPrice.Item(FiatCurrency.Usd.Code, 20000m, 10000m)]);

        var fiatSelector = Substitute.For<IFiatPriceProviderSelector>();
        fiatSelector.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>()).Returns(fullFiat);

        var btcProvider = Substitute.For<IBitcoinPriceProvider>();
        btcProvider.GetAsync().Returns(liveBtc);

        ReplaceService(fiatSelector);
        ReplaceService(btcProvider);

        var job = _serviceProvider.GetRequiredService<LivePricesUpdaterJob>();

        await job.RunAsync(CancellationToken.None);

        WeakReferenceMessenger.Default.Unregister<LivePriceUpdateMessage>(this);

        Assert.That(receivedValue, Is.Not.Null);
        Assert.That(receivedValue!.IsUpToDate, Is.True);
        Assert.That(receivedValue.Fiat.Items, Has.Some.Matches<FiatUsdPrice.Item>(x => x.Currency.Code == FiatCurrency.Usd.Code));
        Assert.That(receivedValue.Fiat.Items, Has.Some.Matches<FiatUsdPrice.Item>(x => x.Currency.Code == FiatCurrency.Brl.Code));
    }
}
