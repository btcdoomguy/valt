using Valt.Core.Common;
using Valt.Core.Common.Exceptions;

namespace Valt.Tests.Domain.Common;

[TestFixture]
public class FiatCurrencyTests
{
    [Test]
    public void Should_Get_Brl_When_Brl_Code_Is_Provided()
    {
        var currency = FiatCurrency.GetFromCode("BRL");
        Assert.That(currency, Is.EqualTo(FiatCurrency.Brl));
    }

    [Test]
    public void Should_Get_Usd_When_Usd_Code_Is_Provided()
    {
        var currency = FiatCurrency.GetFromCode("USD");
        Assert.That(currency, Is.EqualTo(FiatCurrency.Usd));
    }

    [Test]
    public void Should_Get_Eur_When_Eur_Code_Is_Provided()
    {
        var currency = FiatCurrency.GetFromCode("EUR");
        Assert.That(currency, Is.EqualTo(FiatCurrency.Eur));
    }

    [Test]
    public void Should_Throw_InvalidCurrencyCodeException_For_Invalid_Code()
    {
        Assert.Throws<InvalidCurrencyCodeException>(() => FiatCurrency.GetFromCode("XYZ"));
    }

    [Test]
    public void Should_Return_Correct_String_For_Brl()
    {
        var currencyString = FiatCurrency.Brl.ToString();
        Assert.That(currencyString, Is.EqualTo("BRL"));
    }

    [Test]
    public void Should_Return_Correct_String_For_Usd()
    {
        var currencyString = FiatCurrency.Usd.ToString();
        Assert.That(currencyString, Is.EqualTo("USD"));
    }

    [Test]
    public void Should_Return_Correct_String_For_Eur()
    {
        var currencyString = FiatCurrency.Eur.ToString();
        Assert.That(currencyString, Is.EqualTo("EUR"));
    }
}