using System.Reflection;
using NetArchTest.Rules;
using Valt.Core.Kernel.Abstractions;

namespace Valt.Tests.Architecture;

[TestFixture]
public class RepositoriesTests
{
    private static readonly Assembly InfraAssembly = typeof(Valt.Infra.Extensions.Foo).Assembly;
    
    [Test]
    public void RepositoriesImplementations_Should_Be_Internal()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(IRepository))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful);
    }
}