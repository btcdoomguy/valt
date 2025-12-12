using System.Globalization;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;

namespace Valt.Tests.Domain.Common;

/// <summary>
/// Tests for the FiatValue value object.
/// FiatValue represents fiat currency amounts with 2 decimal places precision.
/// </summary>
[TestFixture]
public class FiatValueTests
{
    #region Creation Tests

    [Test]
    public void Should_Throw_InvalidFiatValueException_When_Value_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<InvalidFiatValueException>(() => FiatValue.New(-1m));
    }

    [Test]
    public void Should_Create_FiatValue_With_Given_Value_Rounded_To_Two_Decimals()
    {
        // Arrange & Act: Value with 4 decimals should be rounded to 2
        var fiatValue = FiatValue.New(100.1234m);

        // Assert
        Assert.That(fiatValue.ToString(CultureInfo.InvariantCulture), Is.EqualTo("100.12"));
    }

    [Test]
    public void Should_Create_FiatValue_From_Another_FiatValue()
    {
        // Arrange
        var initialFiatValue = FiatValue.New(100.12m);

        // Act
        var fiatValue = FiatValue.New(initialFiatValue);

        // Assert
        Assert.That(fiatValue.ToString(CultureInfo.InvariantCulture), Is.EqualTo("100.12"));
    }

    [Test]
    public void Should_Return_Empty_FiatValue_As_Zero()
    {
        // Arrange & Act
        var fiatValue = FiatValue.Empty;

        // Assert
        Assert.That(fiatValue.ToString(CultureInfo.InvariantCulture), Is.EqualTo("0.00"));
    }

    #endregion

    #region Arithmetic Operations Tests

    [Test]
    public void Should_Add_Two_FiatValues()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(100.12m);
        var fiatValue2 = FiatValue.New(200.88m);

        // Act
        var result = fiatValue1 + fiatValue2;

        // Assert
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("301.00"));
    }

    [Test]
    public void Should_Subtract_Two_FiatValues()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(300.00m);
        var fiatValue2 = FiatValue.New(200.00m);

        // Act
        var result = fiatValue1 - fiatValue2;

        // Assert
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("100.00"));
    }

    [Test]
    public void Should_Multiply_Two_FiatValues()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(20.00m);
        var fiatValue2 = FiatValue.New(30.00m);

        // Act
        var result = fiatValue1 * fiatValue2;

        // Assert
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("600.00"));
    }

    [Test]
    public void Should_Divide_Two_FiatValues()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(600.00m);
        var fiatValue2 = FiatValue.New(20.00m);

        // Act
        var result = fiatValue1 / fiatValue2;

        // Assert
        Assert.That(result.ToString(CultureInfo.InvariantCulture), Is.EqualTo("30.00"));
    }

    #endregion

    #region Comparison Operators Tests

    [Test]
    public void Should_Return_True_When_Two_FiatValues_Are_Equal()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(100.12m);
        var fiatValue2 = FiatValue.New(100.12m);

        // Act & Assert
        Assert.That(fiatValue1 == fiatValue2, Is.True);
    }

    [Test]
    public void Should_Return_True_When_Two_FiatValues_Are_Not_Equal()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(100.12m);
        var fiatValue2 = FiatValue.New(200.24m);

        // Act & Assert
        Assert.That(fiatValue1 != fiatValue2, Is.True);
    }

    [Test]
    public void Should_Return_True_When_First_FiatValue_Is_Less_Than_Second()
    {
        // Arrange
        var smaller = FiatValue.New(100.12m);
        var larger = FiatValue.New(200.24m);

        // Act & Assert
        Assert.That(smaller < larger, Is.True);
    }

    [Test]
    public void Should_Return_True_When_First_FiatValue_Is_Greater_Than_Second()
    {
        // Arrange
        var larger = FiatValue.New(300.36m);
        var smaller = FiatValue.New(200.24m);

        // Act & Assert
        Assert.That(larger > smaller, Is.True);
    }

    [Test]
    public void Should_Return_True_When_FiatValues_Are_Equal_Using_LessOrEqual()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(200.24m);
        var fiatValue2 = FiatValue.New(200.24m);

        // Act & Assert
        Assert.That(fiatValue1 <= fiatValue2, Is.True);
    }

    [Test]
    public void Should_Return_True_When_FiatValues_Are_Equal_Using_GreaterOrEqual()
    {
        // Arrange
        var fiatValue1 = FiatValue.New(300.36m);
        var fiatValue2 = FiatValue.New(300.36m);

        // Act & Assert
        Assert.That(fiatValue1 >= fiatValue2, Is.True);
    }

    #endregion
}