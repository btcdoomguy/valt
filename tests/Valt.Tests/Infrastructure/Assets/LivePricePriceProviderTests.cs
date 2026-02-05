using Microsoft.Extensions.Logging;
using NSubstitute;
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

    #region Source Tests

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
        var btcPrice = new BtcPrice(
            DateTime.UtcNow,
            true,
            new[]
            {
                new BtcPrice.Item("USD", 50000m),
                new BtcPrice.Item("BRL", 250000m),
                new BtcPrice.Item("EUR", 45000m)
            });

        _bitcoinPriceProvider.GetAsync().Returns(btcPrice);

        // Act
        var result = await _provider.GetPriceAsync("BTC", "USD");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(50000m));
        Assert.That(result.CurrencyCode, Is.EqualTo("USD"));
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Price_For_BTC_BRL()
    {
        // Arrange
        var btcPrice = new BtcPrice(
            DateTime.UtcNow,
            true,
            new[]
            {
                new BtcPrice.Item("USD", 50000m),
                new BtcPrice.Item("BRL", 250000m),
                new BtcPrice.Item("EUR", 45000m)
            });

        _bitcoinPriceProvider.GetAsync().Returns(btcPrice);

        // Act
        var result = await _provider.GetPriceAsync("BTC", "BRL");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(250000m));
        Assert.That(result.CurrencyCode, Is.EqualTo("BRL"));
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Price_For_BTC_EUR()
    {
        // Arrange
        var btcPrice = new BtcPrice(
            DateTime.UtcNow,
            true,
            new[]
            {
                new BtcPrice.Item("USD", 50000m),
                new BtcPrice.Item("BRL", 250000m),
                new BtcPrice.Item("EUR", 45000m)
            });

        _bitcoinPriceProvider.GetAsync().Returns(btcPrice);

        // Act
        var result = await _provider.GetPriceAsync("BTC", "EUR");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(45000m));
        Assert.That(result.CurrencyCode, Is.EqualTo("EUR"));
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
    public async Task GetPriceAsync_Should_Return_Null_When_Currency_Not_Found()
    {
        // Arrange
        var btcPrice = new BtcPrice(
            DateTime.UtcNow,
            true,
            new[]
            {
                new BtcPrice.Item("USD", 50000m),
                new BtcPrice.Item("BRL", 250000m)
            });

        _bitcoinPriceProvider.GetAsync().Returns(btcPrice);

        // Act
        var result = await _provider.GetPriceAsync("BTC", "JPY");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetPriceAsync_Should_Handle_Case_Insensitive_Symbol()
    {
        // Arrange
        var btcPrice = new BtcPrice(
            DateTime.UtcNow,
            true,
            new[]
            {
                new BtcPrice.Item("USD", 50000m)
            });

        _bitcoinPriceProvider.GetAsync().Returns(btcPrice);

        // Act
        var result = await _provider.GetPriceAsync("btc", "USD");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(50000m));
    }

    [Test]
    public async Task GetPriceAsync_Should_Handle_Case_Insensitive_Currency()
    {
        // Arrange
        var btcPrice = new BtcPrice(
            DateTime.UtcNow,
            true,
            new[]
            {
                new BtcPrice.Item("USD", 50000m)
            });

        _bitcoinPriceProvider.GetAsync().Returns(btcPrice);

        // Act
        var result = await _provider.GetPriceAsync("BTC", "usd");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.EqualTo(50000m));
    }

    [Test]
    public async Task GetPriceAsync_Should_Return_Null_When_Provider_Throws()
    {
        // Arrange
        _bitcoinPriceProvider.GetAsync().Returns<BtcPrice>(_ => throw new Exception("Network error"));

        // Act
        var result = await _provider.GetPriceAsync("BTC", "USD");

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region ValidateSymbolAsync Tests

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_True_For_BTC()
    {
        // Act
        var result = await _provider.ValidateSymbolAsync("BTC");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_True_For_BTC_With_Suffix()
    {
        // Act
        var result = await _provider.ValidateSymbolAsync("BTC-USD");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_True_For_Lowercase_BTC()
    {
        // Act
        var result = await _provider.ValidateSymbolAsync("btc");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_False_For_Non_BTC_Symbol()
    {
        // Act
        var result = await _provider.ValidateSymbolAsync("ETH");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateSymbolAsync_Should_Return_False_For_Empty_Symbol()
    {
        // Act
        var result = await _provider.ValidateSymbolAsync("");

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion
}
