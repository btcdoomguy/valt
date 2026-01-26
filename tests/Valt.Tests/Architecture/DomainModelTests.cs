using System.Reflection;
using NetArchTest.Rules;
using Valt.Core.Kernel;

namespace Valt.Tests.Architecture;

[TestFixture]
public class DomainModelTests
{
    private static readonly Assembly CoreAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly AppAssembly = typeof(Valt.App.Extensions.AssemblyMarker).Assembly;
    private static readonly Assembly InfraAssembly = typeof(Valt.Infra.Extensions.Foo).Assembly;

    #region Entity Rules

    [Test]
    public void Entities_Should_Be_In_Core_Layer()
    {
        // Entities should only exist in Core layer, not in Infra or App
        var infraEntities = Types.InAssembly(InfraAssembly)
            .That()
            .Inherit(typeof(Entity<>))
            .GetTypes();

        var appEntities = Types.InAssembly(AppAssembly)
            .That()
            .Inherit(typeof(Entity<>))
            .GetTypes();

        var violatingTypes = infraEntities.Concat(appEntities).Select(t => t.FullName).ToList();

        Assert.That(violatingTypes, Is.Empty,
            () => $"Entities should only be in Core layer. Found in wrong layers: {string.Join(", ", violatingTypes)}");
    }

    [Test]
    public void AggregateRoots_Should_Be_In_Core_Layer()
    {
        var infraAggregates = Types.InAssembly(InfraAssembly)
            .That()
            .Inherit(typeof(AggregateRoot<>))
            .GetTypes();

        var appAggregates = Types.InAssembly(AppAssembly)
            .That()
            .Inherit(typeof(AggregateRoot<>))
            .GetTypes();

        var violatingTypes = infraAggregates.Concat(appAggregates).Select(t => t.FullName).ToList();

        Assert.That(violatingTypes, Is.Empty,
            () => $"Aggregate roots should only be in Core layer. Found in wrong layers: {string.Join(", ", violatingTypes)}");
    }

    #endregion

    #region DTO Placement Rules

    [Test]
    public void DTOs_Should_Not_Be_In_Core_Layer()
    {
        // DTOs (Data Transfer Objects) should be in App or Infra, not Core
        // Exception: Nested types in interfaces (like calculation results) are allowed
        var result = Types.InAssembly(CoreAssembly)
            .That()
            .HaveNameEndingWith("DTO")
            .Or()
            .HaveNameEndingWith("Dto")
            .GetTypes()
            .Where(t => !t.IsNested) // Exclude nested types (often calculation results)
            .ToList();

        var violatingTypes = result.Select(t => t.FullName).ToList();

        Assert.That(violatingTypes, Is.Empty,
            () => $"DTOs should not be in Core layer. Use pure domain objects. Found: {string.Join(", ", violatingTypes)}");
    }

    [Test]
    public void Entities_In_Infra_Should_End_With_Entity()
    {
        // Infra layer uses "Entity" suffix for persistence models (e.g., TransactionEntity)
        // This distinguishes them from domain entities
        var infraTypes = Types.InAssembly(InfraAssembly)
            .That()
            .HaveNameEndingWith("Entity")
            .GetTypes();

        // Verify these are actually in the Infra namespace (sanity check)
        foreach (var type in infraTypes)
        {
            Assert.That(type.Namespace, Does.StartWith("Valt.Infra"),
                $"Entity {type.FullName} should be in Valt.Infra namespace");
        }
    }

    #endregion
}
