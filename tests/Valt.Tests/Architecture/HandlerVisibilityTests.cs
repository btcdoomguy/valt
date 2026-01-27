using System.Reflection;
using NetArchTest.Rules;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Tests.Architecture;

[TestFixture]
public class HandlerVisibilityTests
{
    private static readonly Assembly AppAssembly = typeof(Valt.App.Extensions.AssemblyMarker).Assembly;
    private static readonly Assembly InfraAssembly = typeof(Valt.Infra.Extensions.Foo).Assembly;
    private static readonly Assembly UIAssembly = typeof(Valt.UI.App).Assembly;

    #region Command/Query Handler Visibility

    [Test]
    public void CommandHandlers_In_App_Should_Be_Internal()
    {
        var result = Types.InAssembly(AppAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Command handlers in App should be internal. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void QueryHandlers_In_App_Should_Be_Internal()
    {
        var result = Types.InAssembly(AppAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Query handlers in App should be internal. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void QueryHandlers_In_Infra_Should_Be_Internal()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Query handlers in Infra should be internal. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion


    #region Domain Event Handler Visibility

    [Test]
    public void DomainEventHandlers_Should_Be_Internal()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEventHandler<>))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Domain event handlers should be internal. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Notification Handler Visibility

    [Test]
    public void NotificationHandlers_In_Infra_Should_Be_Internal()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(INotificationHandler<>))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Notification handlers in Infra should be internal. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void NotificationHandlers_In_UI_Should_Be_Internal()
    {
        var result = Types.InAssembly(UIAssembly)
            .That()
            .ImplementInterface(typeof(INotificationHandler<>))
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Notification handlers in UI should be internal. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion
}
