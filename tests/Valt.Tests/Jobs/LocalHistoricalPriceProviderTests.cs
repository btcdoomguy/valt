using CommunityToolkit.Mvvm.Messaging;
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
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Tests.Jobs;

[TestFixture]
public class LocalHistoricalPriceProviderTests : IntegrationTest
{
    protected override Task SeedDatabase()
    {
        // Configure fiat currencies for the tests
        var configManager = _serviceProvider.GetRequiredService<ConfigurationManager>();
        configManager.SetAvailableFiatCurrencies(new[] { FiatCurrency.Usd.Code });

        // Seed price data
        _priceDatabase.GetFiatData().Insert(new FiatDataEntity
        {
            Currency = FiatCurrency.Usd.Code,
            Date = new DateOnly(2023, 1, 1).ToValtDateTime(),
            Price = 1000m
        });
        _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity()
        {
            Date = new DateOnly(2023, 1, 1).ToValtDateTime(),
            Price = 10000m
        });

        return Task.CompletedTask;
    }

    [Test]
    public async Task Should_Get_LivePrices_And_Send_Message()
    {
        LivePriceUpdateMessage receivedValue = null;

        WeakReferenceMessenger.Default.Register<LivePriceUpdateMessage>(
            this,
            (recipient, message) => { receivedValue = message; });

        var job = _serviceProvider.GetRequiredService<LivePricesUpdaterJob>();

        await job.RunAsync(CancellationToken.None);

        WeakReferenceMessenger.Default.Unregister<LivePriceUpdateMessage>(this);

        Assert.That(receivedValue, Is.Not.Null);
    }

    [Test]
    public async Task Should_Get_LastKnownPrices_And_Send_Message()
    {
        LivePriceUpdateMessage receivedValue = null;

        WeakReferenceMessenger.Default.Register<LivePriceUpdateMessage>(
            this,
            (recipient, message) => { receivedValue = message; });

        var failingFiatProvider = Substitute.For<IFiatPriceProvider>();
        failingFiatProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>()).Returns<Task<FiatUsdPrice>>(_ => throw new HttpRequestException("No internet"));

        var failingBtcProvider = Substitute.For<IBitcoinPriceProvider>();
        failingBtcProvider.GetAsync().Returns<Task<BtcPrice>>(_ => throw new TimeoutException("No internet"));

        ReplaceService(failingFiatProvider);
        ReplaceService(failingBtcProvider);

        var job = _serviceProvider.GetRequiredService<LivePricesUpdaterJob>();

        await job.RunAsync(CancellationToken.None);

        WeakReferenceMessenger.Default.Unregister<LivePriceUpdateMessage>(this);

        Assert.That(receivedValue, Is.Not.Null);
        Assert.That(receivedValue.Fiat.UpToDate, Is.False);
        Assert.That(receivedValue.Btc.UpToDate, Is.False);
    }
}