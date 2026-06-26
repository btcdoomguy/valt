using System.Reflection;
using Microsoft.Extensions.Logging;
using NetArchTest.Rules;
using NSubstitute;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Budget.Accounts.Services;

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

    [Test]
    public void AccountTotalsJob_Interval_Should_Be_At_Least_60_Seconds()
    {
        IBackgroundJob job = new AccountTotalsJob(
            Substitute.For<IClock>(),
            Substitute.For<IAccountCacheService>(),
            Substitute.For<IPriceDatabase>(),
            Substitute.For<ILocalDatabase>(),
            Substitute.For<ILogger<AccountTotalsJob>>());

        Assert.That(job.Interval, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(60)));
        Assert.That(job.Interval, Is.EqualTo(TimeSpan.FromSeconds(120)));
    }
}