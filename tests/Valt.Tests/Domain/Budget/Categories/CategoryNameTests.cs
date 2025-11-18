using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Exceptions;

namespace Valt.Tests.Domain.Budget.Categories;

[TestFixture]
public class CategoryNameTests
{
    [Test]
    public void Should_Throw_Error_If_Empty()
    {
        Assert.Throws<EmptyCategoryNameException>(() => CategoryName.New(""));
    }

    [Test]
    public void Should_Throw_Error_If_Name_Bigger_Than_50_Chars()
    {
        Assert.Throws<MaximumFieldLengthException>(() =>
            CategoryName.New("123456789-123456789-123456789-123456789-123456789-1"));
    }

    [Test]
    public void Should_be_equal()
    {
        var instance1 = CategoryName.New("Test");
        var instance2 = CategoryName.New("Test");

        Assert.That(instance2, Is.EqualTo(instance1));
    }
}