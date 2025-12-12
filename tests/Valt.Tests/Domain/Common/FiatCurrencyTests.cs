using Valt.Core.Common;
using Valt.Core.Common.Exceptions;

namespace Valt.Tests.Domain.Common;

/// <summary>
/// Tests for the FiatCurrency value object.
/// FiatCurrency represents supported fiat currencies (BRL, USD, EUR).
/// </summary>
[TestFixture]
public class FiatCurrencyTests
{
    #region GetFromCode Tests

    [Test]
    public void Should_Get_Brl_When_Brl_Code_Is_Provided()
    {
        // Arrange & Act
        var currency = FiatCurrency.GetFromCode("BRL");

        // Assert
        Assert.That(currency, Is.EqualTo(FiatCurrency.Brl));
    }

    [Test]
    public void Should_Get_Usd_When_Usd_Code_Is_Provided()
    {
        // Arrange & Act
        var currency = FiatCurrency.GetFromCode("USD");

        // Assert
        Assert.That(currency, Is.EqualTo(FiatCurrency.Usd));
    }

    [Test]
    public void Should_Get_Eur_When_Eur_Code_Is_Provided()
    {
        // Arrange & Act
        var currency = FiatCurrency.GetFromCode("EUR");

        // Assert
        Assert.That(currency, Is.EqualTo(FiatCurrency.Eur));
    }

    [Test]
    public void Should_Throw_InvalidCurrencyCodeException_For_Invalid_Code()
    {
        // Act & Assert: Invalid currency code should throw
        Assert.Throws<InvalidCurrencyCodeException>(() => FiatCurrency.GetFromCode("XYZ"));
    }

    #endregion

    #region ToString Tests

    [Test]
    public void Should_Return_Correct_String_For_Brl()
    {
        // Arrange & Act
        var currencyString = FiatCurrency.Brl.ToString();

        // Assert
        Assert.That(currencyString, Is.EqualTo("BRL"));
    }

    [Test]
    public void Should_Return_Correct_String_For_Usd()
    {
        // Arrange & Act
        var currencyString = FiatCurrency.Usd.ToString();

        // Assert
        Assert.That(currencyString, Is.EqualTo("USD"));
    }

    [Test]
    public void Should_Return_Correct_String_For_Eur()
    {
        // Arrange & Act
        var currencyString = FiatCurrency.Eur.ToString();

        // Assert
        Assert.That(currencyString, Is.EqualTo("EUR"));
    }

    #endregion
}
