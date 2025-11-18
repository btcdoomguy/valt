using System.Reflection;
using NetArchTest.Rules;
using Valt.Infra.DataAccess.Migrations.Scripts;

namespace Valt.Tests.Architecture;

[TestFixture]
public class MigrationScriptTests
{
    private static readonly Assembly InfraAssembly = typeof(Valt.Infra.Extensions.Foo).Assembly;
    
    [Test]
    public void MigrationScripts_Should_Be_Internal()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(IMigrationScript))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful);
    }
}