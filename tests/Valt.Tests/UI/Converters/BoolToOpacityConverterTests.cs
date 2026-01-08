using System.Globalization;
using Valt.UI.Converters;

namespace Valt.Tests.UI.Converters;

[TestFixture]
public class BoolToOpacityConverterTests
{
    private BoolToOpacityConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new BoolToOpacityConverter();
    }

    #region Convert Tests

    [Test]
    public void Convert_Should_Return_1_When_Value_Is_True()
    {
        // Arrange
        var value = true;

        // Act
        var result = _converter.Convert(value, typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void Convert_Should_Return_0_5_When_Value_Is_False()
    {
        // Arrange
        var value = false;

        // Act
        var result = _converter.Convert(value, typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(0.5));
    }

    [Test]
    public void Convert_Should_Return_1_When_Value_Is_Null()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.Convert(value, typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void Convert_Should_Return_1_When_Value_Is_Not_Boolean()
    {
        // Arrange
        var value = "not a boolean";

        // Act
        var result = _converter.Convert(value, typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void Convert_Should_Return_1_When_Value_Is_Integer()
    {
        // Arrange
        var value = 42;

        // Act
        var result = _converter.Convert(value, typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(1.0));
    }

    #endregion

    #region ConvertBack Tests

    [Test]
    public void ConvertBack_Should_Throw_NotImplementedException()
    {
        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(1.0, typeof(bool), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
