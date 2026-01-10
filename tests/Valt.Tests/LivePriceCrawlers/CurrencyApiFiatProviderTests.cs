using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Kernel.Time;

namespace Valt.Tests.LivePriceCrawlers;

[TestFixture]
public class CurrencyApiFiatProviderTests
{
    [Test]
    public async Task Should_Get_Prices()
    {
        var provider = new CurrencyApiFiatRateProvider(new Clock(), new NullLogger<CurrencyApiFiatRateProvider>());
        var currencies = new[] { FiatCurrency.Brl, FiatCurrency.Eur };

        var prices = await provider.GetAsync(currencies);

        Assert.That(prices.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Brl)!.Price, Is.GreaterThan(0));
        Assert.That(prices.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Eur)!.Price, Is.GreaterThan(0));
    }

    [Test]
    public async Task Should_Return_Usd_When_Only_Usd_Requested()
    {
        var provider = new CurrencyApiFiatRateProvider(new Clock(), new NullLogger<CurrencyApiFiatRateProvider>());
        var currencies = new[] { FiatCurrency.Usd };

        var prices = await provider.GetAsync(currencies);

        Assert.That(prices.Items.Count, Is.EqualTo(1));
        Assert.That(prices.Items.Single().Currency, Is.EqualTo(FiatCurrency.Usd));
        Assert.That(prices.Items.Single().Price, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_Get_All_Supported_Currencies()
    {
        var provider = new CurrencyApiFiatRateProvider(new Clock(), new NullLogger<CurrencyApiFiatRateProvider>());
        var currencies = provider.SupportedCurrencies.ToArray();

        var prices = await provider.GetAsync(currencies);

        Assert.That(prices.UpToDate, Is.True);
        Assert.That(prices.Items.Count, Is.EqualTo(currencies.Length));
        foreach (var currency in currencies)
        {
            var item = prices.Items.SingleOrDefault(x => x.Currency == currency);
            Assert.That(item, Is.Not.Null, $"Currency {currency.Code} not found in response");
            Assert.That(item!.Price, Is.GreaterThan(0), $"Currency {currency.Code} has invalid price");
        }
    }

    [Test]
    public void Should_Support_All_Fiat_Currencies()
    {
        var provider = new CurrencyApiFiatRateProvider(new Clock(), new NullLogger<CurrencyApiFiatRateProvider>());

        Assert.That(provider.SupportedCurrencies.Count, Is.EqualTo(33));
        Assert.That(provider.SupportedCurrencies, Does.Contain(FiatCurrency.Usd));
        Assert.That(provider.SupportedCurrencies, Does.Contain(FiatCurrency.Brl));
        Assert.That(provider.SupportedCurrencies, Does.Contain(FiatCurrency.Eur));
        Assert.That(provider.SupportedCurrencies, Does.Contain(FiatCurrency.Uyu));
        Assert.That(provider.SupportedCurrencies, Does.Contain(FiatCurrency.Pyg));
    }

    [Test]
    public void Should_Have_Correct_Name()
    {
        var provider = new CurrencyApiFiatRateProvider(new Clock(), new NullLogger<CurrencyApiFiatRateProvider>());

        Assert.That(provider.Name, Is.EqualTo("CurrencyApi"));
    }

    [Test]
    public async Task Should_Get_Prices_For_Uyu_And_Pyg()
    {
        var provider = new CurrencyApiFiatRateProvider(new Clock(), new NullLogger<CurrencyApiFiatRateProvider>());
        var currencies = new[] { FiatCurrency.Uyu, FiatCurrency.Pyg };

        var prices = await provider.GetAsync(currencies);

        Assert.That(prices.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Uyu)!.Price, Is.GreaterThan(0));
        Assert.That(prices.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Pyg)!.Price, Is.GreaterThan(0));
    }
}
