using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Exceptions;

namespace Valt.Tests.Domain.Budget.Categories;

/// <summary>
/// Tests for the CategoryName value object.
/// CategoryName enforces validation rules for category names (non-empty, max 50 chars).
/// </summary>
[TestFixture]
public class CategoryNameTests
{
    #region Validation Tests

    [Test]
    public void Should_Throw_Error_If_Empty()
    {
        // Act & Assert: Empty name should throw
        Assert.Throws<EmptyCategoryNameException>(() => CategoryName.New(""));
    }

    [Test]
    public void Should_Throw_Error_If_Name_Bigger_Than_50_Chars()
    {
        // Arrange: Create a name with 51 characters
        var longName = "123456789-123456789-123456789-123456789-123456789-1";

        // Act & Assert: Should throw MaximumFieldLengthException
        Assert.Throws<MaximumFieldLengthException>(() => CategoryName.New(longName));
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Should_Be_Equal_When_Values_Are_Same()
    {
        // Arrange
        var instance1 = CategoryName.New("Test");
        var instance2 = CategoryName.New("Test");

        // Assert: Two CategoryNames with same value should be equal
        Assert.That(instance2, Is.EqualTo(instance1));
    }

    #endregion
}
