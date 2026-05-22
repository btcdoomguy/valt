using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Tests.Domain.Budget.Accounts;

/// <summary>
/// Tests for AccountGroupTotalCurrency value object.
/// </summary>
[TestFixture]
public class AccountGroupTotalCurrencyTests
{
    [Test]
    public void DefaultFiat_Should_Create_DefaultFiat_Type()
    {
        var currency = AccountGroupTotalCurrency.DefaultFiat();

        Assert.Multiple(() =>
        {
            Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
            Assert.That(currency.CurrencyCode, Is.Null);
        });
    }

    [Test]
    public void Bitcoin_Should_Create_Bitcoin_Type()
    {
        var currency = AccountGroupTotalCurrency.Bitcoin();

        Assert.Multiple(() =>
        {
            Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.Bitcoin));
            Assert.That(currency.CurrencyCode, Is.Null);
        });
    }

    [Test]
    public void Fiat_WithValidCode_Should_Create_SpecificFiat_Type()
    {
        var currency = AccountGroupTotalCurrency.Fiat("USD");

        Assert.Multiple(() =>
        {
            Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.SpecificFiat));
            Assert.That(currency.CurrencyCode, Is.EqualTo("USD"));
        });
    }

    [Test]
    public void Fiat_WithEmptyCode_Should_Throw_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AccountGroupTotalCurrency.Fiat(""));
    }

    [Test]
    public void Fiat_WithInvalidCode_Should_Throw_InvalidCurrencyCodeException()
    {
        Assert.Throws<Valt.Core.Common.Exceptions.InvalidCurrencyCodeException>(() => AccountGroupTotalCurrency.Fiat("INVALID"));
    }

    [Test]
    public void ToStorageString_DefaultFiat_Should_Return_DEFAULT()
    {
        var currency = AccountGroupTotalCurrency.DefaultFiat();
        Assert.That(currency.ToStorageString(), Is.EqualTo("DEFAULT"));
    }

    [Test]
    public void ToStorageString_Bitcoin_Should_Return_BTC()
    {
        var currency = AccountGroupTotalCurrency.Bitcoin();
        Assert.That(currency.ToStorageString(), Is.EqualTo("BTC"));
    }

    [Test]
    public void ToStorageString_SpecificFiat_Should_Return_CurrencyCode()
    {
        var currency = AccountGroupTotalCurrency.Fiat("BRL");
        Assert.That(currency.ToStorageString(), Is.EqualTo("BRL"));
    }

    [Test]
    public void FromStorageString_WithDEFAULT_Should_Return_DefaultFiat()
    {
        var currency = AccountGroupTotalCurrency.FromStorageString("DEFAULT");
        Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }

    [Test]
    public void FromStorageString_WithEmpty_Should_Return_DefaultFiat()
    {
        var currency = AccountGroupTotalCurrency.FromStorageString("");
        Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }

    [Test]
    public void FromStorageString_WithBTC_Should_Return_Bitcoin()
    {
        var currency = AccountGroupTotalCurrency.FromStorageString("BTC");
        Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.Bitcoin));
    }

    [Test]
    public void FromStorageString_WithValidFiat_Should_Return_SpecificFiat()
    {
        var currency = AccountGroupTotalCurrency.FromStorageString("EUR");
        Assert.Multiple(() =>
        {
            Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.SpecificFiat));
            Assert.That(currency.CurrencyCode, Is.EqualTo("EUR"));
        });
    }

    [Test]
    public void FromStorageString_WithInvalidFiat_Should_Return_DefaultFiat()
    {
        var currency = AccountGroupTotalCurrency.FromStorageString("INVALID");
        Assert.That(currency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }

    [Test]
    public void IsAvailable_DefaultFiat_Should_Be_True()
    {
        var currency = AccountGroupTotalCurrency.DefaultFiat();
        Assert.That(currency.IsAvailable(), Is.True);
    }

    [Test]
    public void IsAvailable_Bitcoin_Should_Be_True()
    {
        var currency = AccountGroupTotalCurrency.Bitcoin();
        Assert.That(currency.IsAvailable(), Is.True);
    }

    [Test]
    public void IsAvailable_SpecificFiat_WithValidCode_Should_Be_True()
    {
        var currency = AccountGroupTotalCurrency.Fiat("USD");
        Assert.That(currency.IsAvailable(), Is.True);
    }

    [Test]
    public void IsAvailable_SpecificFiat_WithUnsupportedCode_Should_Be_False()
    {
        // Simulate an edge case: a currency that was once valid but is no longer supported
        // This can happen if a currency is removed from the system after being saved
        var currency = AccountGroupTotalCurrency.FromStorageString("DEFAULT");
        // The fallback should make it available
        Assert.That(currency.IsAvailable(), Is.True);
    }

    [Test]
    public void IsAvailable_SpecificFiat_WithRestrictedAvailableList_Should_Check_List()
    {
        var currency = AccountGroupTotalCurrency.Fiat("BRL");
        var available = new List<string> { "USD", "EUR" };

        Assert.That(currency.IsAvailable(available), Is.False);
    }

    [Test]
    public void IsAvailable_SpecificFiat_InAvailableList_Should_Be_True()
    {
        var currency = AccountGroupTotalCurrency.Fiat("BRL");
        var available = new List<string> { "USD", "BRL" };

        Assert.That(currency.IsAvailable(available), Is.True);
    }

    [Test]
    public void FallbackToDefaultIfUnavailable_WhenAvailable_Should_Return_Self()
    {
        var currency = AccountGroupTotalCurrency.Fiat("USD");
        var result = currency.FallbackToDefaultIfUnavailable();
        Assert.That(result, Is.EqualTo(currency));
    }

    [Test]
    public void FallbackToDefaultIfUnavailable_WhenUnavailable_Should_Return_DefaultFiat()
    {
        var currency = AccountGroupTotalCurrency.FromStorageString("INVALID");
        var result = currency.FallbackToDefaultIfUnavailable();
        Assert.That(result.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }

    [Test]
    public void FallbackToDefaultIfUnavailable_WithRestrictedList_NotInList_Should_Return_DefaultFiat()
    {
        var currency = AccountGroupTotalCurrency.Fiat("BRL");
        var available = new List<string> { "USD", "EUR" };
        var result = currency.FallbackToDefaultIfUnavailable(available);
        Assert.That(result.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }

    [Test]
    public void FallbackToDefaultIfUnavailable_WithRestrictedList_InList_Should_Return_Self()
    {
        var currency = AccountGroupTotalCurrency.Fiat("BRL");
        var available = new List<string> { "USD", "BRL", "EUR" };
        var result = currency.FallbackToDefaultIfUnavailable(available);
        Assert.That(result, Is.EqualTo(currency));
    }
}
