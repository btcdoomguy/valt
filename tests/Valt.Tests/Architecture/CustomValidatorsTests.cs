using System.ComponentModel.DataAnnotations;
using System.Reflection;
using NetArchTest.Rules;

namespace Valt.Tests.Architecture;

[TestFixture]
public class CustomValidatorsTests
{
    private static readonly Assembly UIAssembly = typeof(Valt.UI.App).Assembly;
    
    [Test]
    public void ValidationAttributes_Should_Be_Internal()
    {
        var result = Types.InAssembly(UIAssembly)
            .That()
            .Inherit(typeof(ValidationAttribute))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful);
    }
}