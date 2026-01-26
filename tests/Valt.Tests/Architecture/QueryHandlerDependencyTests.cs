using System.Reflection;
using NetArchTest.Rules;
using Valt.App.Kernel.Queries;
using Valt.Core.Kernel.Abstractions;

namespace Valt.Tests.Architecture;

/// <summary>
/// Tests that enforce query handlers use query interfaces, not repositories.
/// Query handlers are for read operations and should use IXxxQueries interfaces,
/// while repositories are for write operations (commands).
/// </summary>
[TestFixture]
public class QueryHandlerDependencyTests
{
    private static readonly Assembly AppAssembly = typeof(Valt.App.Extensions.AssemblyMarker).Assembly;

    [Test]
    public void QueryHandlers_Should_Not_Depend_On_Repositories()
    {
        // Get all types that implement IQueryHandler
        var queryHandlerTypes = Types.InAssembly(AppAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .GetTypes();

        var violatingTypes = new List<string>();

        foreach (var handlerType in queryHandlerTypes)
        {
            // Check constructor parameters for repository dependencies
            var constructors = handlerType.GetConstructors();
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                foreach (var parameter in parameters)
                {
                    var paramType = parameter.ParameterType;

                    // Check if the parameter type implements IRepository
                    if (typeof(IRepository).IsAssignableFrom(paramType))
                    {
                        violatingTypes.Add($"{handlerType.Name} depends on {paramType.Name}");
                    }
                }
            }
        }

        Assert.That(violatingTypes, Is.Empty,
            () => $"Query handlers should not depend on IRepository interfaces. Use IXxxQueries interfaces instead.\n" +
                  $"Violating handlers:\n  - {string.Join("\n  - ", violatingTypes)}");
    }
}
