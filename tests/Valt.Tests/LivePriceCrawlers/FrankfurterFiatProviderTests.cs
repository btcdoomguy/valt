using Microsoft.Extensions.Logging.Abstractions;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Kernel.Time;

namespace Valt.Tests.LivePriceCrawlers;

[TestFixture]
public class FrankfurterFiatProviderTests
{
    [Test]
    public async Task Should_Get_Prices()
    {
        var frankfurterUsdRateProvider = new FrankfurterFiatRateProvider(new Clock(), new NullLogger<FrankfurterFiatRateProvider>());
        var currencies = new[] { FiatCurrency.Brl.Code, FiatCurrency.Eur.Code };

        var prices = await frankfurterUsdRateProvider.GetAsync(currencies);

        Assert.That(prices.Items.SingleOrDefault(x => x.CurrencyCode == FiatCurrency.Brl.Code)!.Price, Is.GreaterThan(0));
        Assert.That(prices.Items.SingleOrDefault(x => x.CurrencyCode == FiatCurrency.Eur.Code)!.Price, Is.GreaterThan(0));
    }
}