using System.Reflection;
using NetArchTest.Rules;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Tests.Architecture;

[TestFixture]
public class BackgroundJobsTests
{
    private static readonly Assembly InfraAssembly = typeof(Valt.Infra.Extensions.Foo).Assembly;
    
    [Test]
    public void BackgroundJobs_Should_Be_Internal()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(IBackgroundJob))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful);
    }
}