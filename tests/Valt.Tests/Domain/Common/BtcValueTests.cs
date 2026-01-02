using Valt.Core.Common;
using Valt.Core.Common.Exceptions;

namespace Valt.Tests.Domain.Common;

/// <summary>
/// Tests for the BtcValue value object.
/// BtcValue represents Bitcoin amounts stored internally as satoshis (1 BTC = 100,000,000 sats).
/// </summary>
[TestFixture]
public class BtcValueTests
{
    #region Creation Tests

    [Test]
    public void Should_Throw_InvalidBtcValueException_When_Sats_Are_Negative()
    {
        // Act & Assert
        Assert.Throws<InvalidBtcValueException>(() => BtcValue.New(-1));
    }

    [Test]
    public void Should_Create_BtcValue_With_Given_Sats()
    {
        // Arrange & Act
        var btcValue = BtcValue.New(100000);

        // Assert
        Assert.That(btcValue.ToString(), Is.EqualTo("100000"));
    }

    [Test]
    public void Should_Create_BtcValue_From_Another_BtcValue()
    {
        // Arrange
        var initialBtcValue = BtcValue.New(100000);

        // Act
        var btcValue = BtcValue.New(initialBtcValue);

        // Assert
        Assert.That(btcValue.ToString(), Is.EqualTo("100000"));
    }

    [Test]
    public void Should_Return_Empty_BtcValue_As_Zero()
    {
        // Arrange & Act
        var btcValue = BtcValue.Empty;

        // Assert
        Assert.That(btcValue.ToString(), Is.EqualTo("0"));
    }

    #endregion

    #region Conversion Tests

    [Test]
    public void Should_Convert_Sats_To_Bitcoin_String()
    {
        // Arrange: 100,000,000 sats = 1 BTC
        var btcValue = BtcValue.New(100000000);

        // Act & Assert
        Assert.That(btcValue.ToBitcoinString(), Is.EqualTo("1"));
    }

    #endregion

    #region Arithmetic Operations Tests

    [Test]
    public void Should_Add_Two_BtcValues()
    {
        // Arrange
        var btcValue1 = BtcValue.New(100000);
        var btcValue2 = BtcValue.New(200000);

        // Act
        var result = btcValue1 + btcValue2;

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("300000"));
    }

    [Test]
    public void Should_Subtract_Two_BtcValues()
    {
        // Arrange
        var btcValue1 = BtcValue.New(300000);
        var btcValue2 = BtcValue.New(200000);

        // Act
        var result = btcValue1 - btcValue2;

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("100000"));
    }

    [Test]
    public void Should_Multiply_Two_BtcValues()
    {
        // Arrange
        var btcValue1 = BtcValue.New(2000);
        var btcValue2 = BtcValue.New(3000);

        // Act
        var result = btcValue1 * btcValue2;

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("6000000"));
    }

    [Test]
    public void Should_Divide_Two_BtcValues()
    {
        // Arrange
        var btcValue1 = BtcValue.New(6000000);
        var btcValue2 = BtcValue.New(2000);

        // Act
        var result = btcValue1 / btcValue2;

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("3000"));
    }

    #endregion

    #region Comparison Operators Tests

    [Test]
    public void Should_Return_True_When_Two_BtcValues_Are_Equal()
    {
        // Arrange
        var btcValue1 = BtcValue.New(100000);
        var btcValue2 = BtcValue.New(100000);

        // Act & Assert
        Assert.That(btcValue1 == btcValue2, Is.True);
    }

    [Test]
    public void Should_Return_True_When_Two_BtcValues_Are_Not_Equal()
    {
        // Arrange
        var btcValue1 = BtcValue.New(100000);
        var btcValue2 = BtcValue.New(200000);

        // Act & Assert
        Assert.That(btcValue1 != btcValue2, Is.True);
    }

    [Test]
    public void Should_Return_True_When_First_BtcValue_Is_Less_Than_Second()
    {
        // Arrange
        var smaller = BtcValue.New(100000);
        var larger = BtcValue.New(200000);

        // Act & Assert
        Assert.That(smaller < larger, Is.True);
    }

    [Test]
    public void Should_Return_True_When_First_BtcValue_Is_Greater_Than_Second()
    {
        // Arrange
        var larger = BtcValue.New(300000);
        var smaller = BtcValue.New(200000);

        // Act & Assert
        Assert.That(larger > smaller, Is.True);
    }

    [Test]
    public void Should_Return_True_When_BtcValues_Are_Equal_Using_LessOrEqual()
    {
        // Arrange
        var btcValue1 = BtcValue.New(200000);
        var btcValue2 = BtcValue.New(200000);

        // Act & Assert
        Assert.That(btcValue1 <= btcValue2, Is.True);
    }

    [Test]
    public void Should_Return_True_When_BtcValues_Are_Equal_Using_GreaterOrEqual()
    {
        // Arrange
        var btcValue1 = BtcValue.New(300000);
        var btcValue2 = BtcValue.New(300000);

        // Act & Assert
        Assert.That(btcValue1 >= btcValue2, Is.True);
    }

    #endregion
}