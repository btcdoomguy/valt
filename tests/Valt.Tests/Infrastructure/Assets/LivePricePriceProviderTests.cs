using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Valt.Core.Modules.Assets;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.Modules.Assets.PriceProviders;

namespace Valt.Tests.Infrastructure.Assets;

[TestFixture]
public class LivePricePriceProviderTests
{
    private IBitcoinPriceProvider _bitcoinPriceProvider;
    private ILogger<LivePricePriceProvider> _logger;
    private LivePricePriceProvider _provider;

    [SetUp]
    public void SetUp()
    {
        _bitcoinPriceProvider = Substitute.For<IBitcoinPriceProvider>();
        _logger = Substitute.For<ILogger<LivePricePriceProvider>>();
        _provider = new LivePricePriceProvider(_bitcoinPriceProvider, _logger);
    }

    #region Source Property Tests

    [Test]
    public void Source_Should_Return_LivePrice()
    {
        // Assert
        Assert.That(_provider.Source, Is.EqualTo(AssetPriceSource.LivePrice));
    }

    #endregion

    #region GetPriceAsync Tests

    [Test]
    public async Task GetPriceAsync_Should_Return_Price_For_BTC_USD()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var btcPrice = new BtcPrice(utcNow, true, new[]
        {
            new BtcPrice.Item("USD", 50000m)
        });
        _bitcoinPriceProvider.GetAsync().Returns(Task.FromResult(btcPrice));

        // Act
        var result = await _provider.GetPriceAsync("BTC", "USD");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(50000m));
        Assert.That(result.CurrencyCode, Is.EqualTo("USD"));
        Assert.That(result.FetchedAt, Is.EqualTo(utcNow));
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Price_For_Lowercase_BTC_USD()
    {
        // Arrange
        var btcPrice = new BtcPrice(DateTime.UtcNow, true, new[]
        {
            new BtcPrice.Item("USD", 50000m)
        });
        _bitcoinPriceProvider.GetAsync().Returns(Task.FromResult(btcPrice));

        // Act
        var result = await _provider.GetPriceAsync("btc", "usd");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(50000m));
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Null_For_Non_BTC_Symbol()
    {
        // Act
        var result = await _provider.GetPriceAsync("ETH", "USD");

        // Assert
        Assert.That(result, Is.Null);
        await _bitcoinPriceProvider.DidNotReceive().GetAsync();
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Null_For_Non_USD_Currency()
    {
        // Act
        var result = await _provider.GetPriceAsync("BTC", "EUR");

        // Assert
        Assert.That(result, Is.Null);
        await _bitcoinPriceProvider.DidNotReceive().GetAsync();
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Null_For_Non_BTC_And_Non_USD()
    {
        // Act
        var result = await _provider.GetPriceAsync("ETH", "EUR");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Null_When_USD_Not_In_Items()
    {
        // Arrange
        var btcPrice = new BtcPrice(DateTime.UtcNow, true, new[]
        {
            new BtcPrice.Item("EUR", 45000m),
            new BtcPrice.Item("BRL", 250000m)
        });
        _bitcoinPriceProvider.GetAsync().Returns(Task.FromResult(btcPrice));

        // Act
        var result = await _provider.GetPriceAsync("BTC", "USD");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Null_On_Exception()
    {
        // Arrange
        _bitcoinPriceProvider.GetAsync().ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _provider.GetPriceAsync("BTC", "USD");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetPriceAsync_Should_Find_USD_Case_Insensitively()
    {
        // Arrange
        var btcPrice = new BtcPrice(DateTime.UtcNow, true, new[]
        {
            new BtcPrice.Item("usd", 50000m) // lowercase
        });
        _bitcoinPriceProvider.GetAsync().Returns(Task.FromResult(btcPrice));

        // Act
        var result = await _provider.GetPriceAsync("BTC", "USD");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(50000m));
    }

    #endregion

    #region ValidateSymbolAsync Tests

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_True_For_BTC()
    {
        // Act
        var isValid = await _provider.ValidateSymbolAsync("BTC");

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_True_For_Lowercase_BTC()
    {
        // Act
        var isValid = await _provider.ValidateSymbolAsync("btc");

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_True_For_MixedCase_BTC()
    {
        // Act
        var isValid = await _provider.ValidateSymbolAsync("Btc");

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_False_For_Other_Symbols()
    {
        // Act & Assert
        Assert.That(await _provider.ValidateSymbolAsync("ETH"), Is.False);
        Assert.That(await _provider.ValidateSymbolAsync("AAPL"), Is.False);
        Assert.That(await _provider.ValidateSymbolAsync("bitcoin"), Is.False);
        Assert.That(await _provider.ValidateSymbolAsync(""), Is.False);
        Assert.That(await _provider.ValidateSymbolAsync("BTC-USD"), Is.False);
    }

    #endregion
}
