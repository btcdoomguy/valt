using System.Globalization;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;

namespace Valt.Tests.Domain.Common;

[TestFixture]
public class FiatValueTests
{
    [Test]
    public void Should_Throw_InvalidFiatValueException_When_Value_Is_Negative()
    {
        Assert.Throws<InvalidFiatValueException>(() => FiatValue.New(-1m));
    }

    [Test]
    public void Should_Create_FiatValue_With_Given_Value_Rounded()
    {
        var fiatValue = FiatValue.New(100.1234m);
        Assert.That(fiatValue.ToString(CultureInfo.InvariantCulture), Is.EqualTo("100.12"));
    }

    [Test]
    public void Should_Create_FiatValue_From_Another_FiatValue()
    {
        var initialFiatValue = FiatValue.New(100.12m);
        var fiatValue = FiatValue.New(initialFiatValue);
        Assert.That(fiatValue.ToString(CultureInfo.InvariantCulture), Is.EqualTo("100.12"));
    }

    [Test]
    public void Should_Return_Empty_FiatValue_As_Zero()
    {
        var fiatValue = FiatValue.Empty;
        Assert.That(fiatValue.ToString(CultureInfo.InvariantCulture), Is.EqualTo("0.00"));
    }

    [Test]
    public void Should_Add_Two_FiatValues()
    {
        var fiatValue1 = FiatValue.New(100.12m);
        var fiatValue2 = FiatValue.New(200.88m);
        var result = fiatValue1 + fiatValue2;
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("301.00"));
    }

    [Test]
    public void Should_Subtract_Two_FiatValues()
    {
        var fiatValue1 = FiatValue.New(300.00m);
        var fiatValue2 = FiatValue.New(200.00m);
        var result = fiatValue1 - fiatValue2;
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("100.00"));
    }

    [Test]
    public void Should_Multiply_Two_FiatValues()
    {
        var fiatValue1 = FiatValue.New(20.00m);
        var fiatValue2 = FiatValue.New(30.00m);
        var result = fiatValue1 * fiatValue2;
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("600.00"));
    }

    [Test]
    public void Should_Divide_Two_FiatValues()
    {
        var fiatValue1 = FiatValue.New(600.00m);
        var fiatValue2 = FiatValue.New(20.00m);
        var result = fiatValue1 / fiatValue2;
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("30.00"));
    }

    [Test]
    public void Should_Check_If_Two_FiatValues_Are_Equal()
    {
        var fiatValue1 = FiatValue.New(100.12m);
        var fiatValue2 = FiatValue.New(100.12m);
        Assert.That(fiatValue1 == fiatValue2);
    }

    [Test]
    public void Should_Check_If_Two_FiatValues_Are_Not_Equal()
    {
        var fiatValue1 = FiatValue.New(100.12m);
        var fiatValue2 = FiatValue.New(200.24m);
        Assert.That(fiatValue1 != fiatValue2);
    }

    [Test]
    public void Should_Check_If_One_FiatValue_Is_Less_Than_Another()
    {
        var fiatValue1 = FiatValue.New(100.12m);
        var fiatValue2 = FiatValue.New(200.24m);
        Assert.That(fiatValue1 < fiatValue2);
    }

    [Test]
    public void Should_Check_If_One_FiatValue_Is_Greater_Than_Another()
    {
        var fiatValue1 = FiatValue.New(300.36m);
        var fiatValue2 = FiatValue.New(200.24m);
        Assert.That(fiatValue1 > fiatValue2);
    }

    [Test]
    public void Should_Check_If_One_FiatValue_Is_Less_Than_Or_Equal_To_Another()
    {
        var fiatValue1 = FiatValue.New(200.24m);
        var fiatValue2 = FiatValue.New(200.24m);
        Assert.That(fiatValue1 <= fiatValue2);
    }

    [Test]
    public void Should_Check_If_One_FiatValue_Is_Greater_Than_Or_Equal_To_Another()
    {
        var fiatValue1 = FiatValue.New(300.36m);
        var fiatValue2 = FiatValue.New(300.36m);
        Assert.That(fiatValue1 >= fiatValue2);
    }
}