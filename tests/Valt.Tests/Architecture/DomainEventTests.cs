using System.Reflection;
using NetArchTest.Rules;
using Valt.Core.Kernel;
using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Tests.Architecture;

[TestFixture]
public class DomainEventTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;

    [Test]
    public void DomainEvents_Should_Be_Sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .BeSealed()
            .GetResult();

        Assert.That(result.IsSuccessful);
    }

    [Test]
    public void DomainEvents_Should_End_With_Event_Postfix()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .HaveNameEndingWith("Event")
            .GetResult();

        Assert.That(result.IsSuccessful);
    }
}