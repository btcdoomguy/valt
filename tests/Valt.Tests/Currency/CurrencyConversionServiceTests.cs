using Valt.Infra.Modules.Currency.Services;

namespace Valt.Tests.Currency;

/// <summary>
/// Tests for the CurrencyConversionService that handles fiat and BTC conversions.
/// Uses USD as an intermediate currency for fiat-to-fiat conversions.
/// </summary>
[TestFixture]
public class CurrencyConversionServiceTests
{
    private CurrencyConversionService _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new CurrencyConversionService();
    }

    #region Same Currency Tests

    [Test]
    public void Convert_SameCurrency_ReturnsSameAmount()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.Convert(100m, "USD", "USD", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(100m));
    }

    [Test]
    public void Convert_SameCurrency_BTC_ReturnsSameAmount()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(1.5m, "BTC", "BTC", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(1.5m));
    }

    #endregion

    #region Zero Amount Tests

    [Test]
    public void Convert_ZeroAmount_ReturnsZero()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.Convert(0m, "USD", "BRL", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    #endregion

    #region Fiat to Fiat Conversion Tests

    [Test]
    public void Convert_UsdToBrl_ReturnsCorrectAmount()
    {
        // Arrange: BRL rate of 5 means 1 USD = 5 BRL
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.Convert(100m, "USD", "BRL", 50000m, fiatRates);

        // Assert: 100 USD * 5 = 500 BRL
        Assert.That(result, Is.EqualTo(500m));
    }

    [Test]
    public void Convert_BrlToUsd_ReturnsCorrectAmount()
    {
        // Arrange: BRL rate of 5 means 1 USD = 5 BRL
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.Convert(500m, "BRL", "USD", 50000m, fiatRates);

        // Assert: 500 BRL / 5 = 100 USD
        Assert.That(result, Is.EqualTo(100m));
    }

    [Test]
    public void Convert_BrlToEur_CrossCurrency_ReturnsCorrectAmount()
    {
        // Arrange: BRL = 5, EUR = 0.92 (relative to USD)
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m }, { "EUR", 0.92m } };

        // Act: 500 BRL -> USD -> EUR
        var result = _sut.Convert(500m, "BRL", "EUR", 50000m, fiatRates);

        // Assert: 500 BRL / 5 = 100 USD * 0.92 = 92 EUR
        Assert.That(result, Is.EqualTo(92m));
    }

    #endregion

    #region BTC to Fiat Conversion Tests

    [Test]
    public void Convert_BtcToUsd_ReturnsCorrectAmount()
    {
        // Arrange: Bitcoin price is 50000 USD
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(1m, "BTC", "USD", 50000m, fiatRates);

        // Assert: 1 BTC * 50000 = 50000 USD
        Assert.That(result, Is.EqualTo(50000m));
    }

    [Test]
    public void Convert_BtcToBrl_ReturnsCorrectAmount()
    {
        // Arrange: Bitcoin price is 50000 USD, BRL rate is 5
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.Convert(1m, "BTC", "BRL", 50000m, fiatRates);

        // Assert: 1 BTC * 50000 USD * 5 = 250000 BRL
        Assert.That(result, Is.EqualTo(250000m));
    }

    [Test]
    public void Convert_FractionalBtcToUsd_ReturnsCorrectAmount()
    {
        // Arrange: Bitcoin price is 50000 USD
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act: 0.01 BTC
        var result = _sut.Convert(0.01m, "BTC", "USD", 50000m, fiatRates);

        // Assert: 0.01 BTC * 50000 = 500 USD
        Assert.That(result, Is.EqualTo(500m));
    }

    #endregion

    #region Fiat to BTC Conversion Tests

    [Test]
    public void Convert_UsdToBtc_ReturnsCorrectAmount()
    {
        // Arrange: Bitcoin price is 50000 USD
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(50000m, "USD", "BTC", 50000m, fiatRates);

        // Assert: 50000 USD / 50000 = 1 BTC
        Assert.That(result, Is.EqualTo(1m));
    }

    [Test]
    public void Convert_BrlToBtc_ReturnsCorrectAmount()
    {
        // Arrange: Bitcoin price is 50000 USD, BRL rate is 5
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.Convert(250000m, "BRL", "BTC", 50000m, fiatRates);

        // Assert: 250000 BRL / 5 = 50000 USD / 50000 = 1 BTC
        Assert.That(result, Is.EqualTo(1m));
    }

    [Test]
    public void Convert_SmallFiatToBtc_ReturnsCorrectAmount()
    {
        // Arrange: Bitcoin price is 50000 USD
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act: 500 USD
        var result = _sut.Convert(500m, "USD", "BTC", 50000m, fiatRates);

        // Assert: 500 USD / 50000 = 0.01 BTC
        Assert.That(result, Is.EqualTo(0.01m));
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void Convert_ZeroBtcPrice_ReturnsZero()
    {
        // Arrange: Bitcoin price is 0
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(100m, "USD", "BTC", 0m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void Convert_NullBtcPrice_ReturnsZero()
    {
        // Arrange: Bitcoin price is null
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(100m, "USD", "BTC", null, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void Convert_MissingCurrencyRate_ReturnsZero()
    {
        // Arrange: No BRL rate defined
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(100m, "USD", "BRL", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void Convert_NullFiatRates_ReturnsZero()
    {
        // Arrange: FiatRates is null

        // Act
        var result = _sut.Convert(100m, "BRL", "USD", 50000m, null);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void Convert_CaseInsensitiveCurrencyCode()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.Convert(100m, "usd", "brl", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(500m));
    }

    [Test]
    public void Convert_BtcCaseInsensitive()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(1m, "btc", "USD", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(50000m));
    }

    #endregion

    #region SATS Conversion Tests

    [Test]
    public void Convert_SatsToSats_ReturnsSameAmount()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(100000m, "SATS", "SATS", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(100000m));
    }

    [Test]
    public void Convert_SatsToBtc_ReturnsCorrectAmount()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act: 100,000 sats = 0.001 BTC
        var result = _sut.Convert(100000m, "SATS", "BTC", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(0.001m));
    }

    [Test]
    public void Convert_BtcToSats_ReturnsCorrectAmount()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act: 0.001 BTC = 100,000 sats
        var result = _sut.Convert(0.001m, "BTC", "SATS", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(100000m));
    }

    [Test]
    public void Convert_SatsToUsd_ReturnsCorrectAmount()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act: 100,000 sats = 0.001 BTC * 50000 USD = 50 USD
        var result = _sut.Convert(100000m, "SATS", "USD", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(50m));
    }

    [Test]
    public void Convert_UsdToSats_ReturnsCorrectAmount()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act: 50 USD / 50000 = 0.001 BTC = 100,000 sats
        var result = _sut.Convert(50m, "USD", "SATS", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(100000m));
    }

    [Test]
    public void Convert_SatsCaseInsensitive()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m } };

        // Act
        var result = _sut.Convert(100000m, "sats", "btc", 50000m, fiatRates);

        // Assert
        Assert.That(result, Is.EqualTo(0.001m));
    }

    #endregion

    #region ConvertToAll Tests

    [Test]
    public void ConvertToAll_ReturnsAllCurrencies()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal>
        {
            { "USD", 1m },
            { "BRL", 5m },
            { "EUR", 0.92m }
        };

        // Act
        var result = _sut.ConvertToAll(100m, "USD", 50000m, fiatRates);

        // Assert
        Assert.That(result.ContainsKey("BTC"), Is.True);
        Assert.That(result.ContainsKey("USD"), Is.True);
        Assert.That(result.ContainsKey("BRL"), Is.True);
        Assert.That(result.ContainsKey("EUR"), Is.True);

        // Verify some values
        Assert.That(result["USD"], Is.EqualTo(100m));
        Assert.That(result["BRL"], Is.EqualTo(500m));
        Assert.That(result["EUR"], Is.EqualTo(92m));
        Assert.That(result["BTC"], Is.EqualTo(0.002m));
    }

    [Test]
    public void ConvertToAll_FromBtc_ReturnsAllCurrencies()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal>
        {
            { "USD", 1m },
            { "BRL", 5m }
        };

        // Act
        var result = _sut.ConvertToAll(1m, "BTC", 50000m, fiatRates);

        // Assert
        Assert.That(result["BTC"], Is.EqualTo(1m));
        Assert.That(result["USD"], Is.EqualTo(50000m));
        Assert.That(result["BRL"], Is.EqualTo(250000m));
    }

    [Test]
    public void ConvertToAll_IncludesSats()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act
        var result = _sut.ConvertToAll(1m, "BTC", 50000m, fiatRates);

        // Assert
        Assert.That(result.ContainsKey("SATS"), Is.True);
        Assert.That(result["SATS"], Is.EqualTo(100000000m)); // 1 BTC = 100M sats
    }

    [Test]
    public void ConvertToAll_FromSats_ReturnsAllCurrencies()
    {
        // Arrange
        var fiatRates = new Dictionary<string, decimal> { { "USD", 1m }, { "BRL", 5m } };

        // Act: 100,000 sats = 0.001 BTC
        var result = _sut.ConvertToAll(100000m, "SATS", 50000m, fiatRates);

        // Assert
        Assert.That(result["SATS"], Is.EqualTo(100000m));
        Assert.That(result["BTC"], Is.EqualTo(0.001m));
        Assert.That(result["USD"], Is.EqualTo(50m)); // 0.001 BTC * 50000
        Assert.That(result["BRL"], Is.EqualTo(250m)); // 50 USD * 5
    }

    #endregion
}
