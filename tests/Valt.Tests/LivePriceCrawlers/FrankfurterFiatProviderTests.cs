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
        var currencies = new[] { FiatCurrency.Brl, FiatCurrency.Eur };

        var prices = await frankfurterUsdRateProvider.GetAsync(currencies);

        Assert.That(prices.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Brl)!.Price, Is.GreaterThan(0));
        Assert.That(prices.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Eur)!.Price, Is.GreaterThan(0));
    }
}