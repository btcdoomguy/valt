using System.Reflection;
using NetArchTest.Rules;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.Core.Kernel;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Infra.Kernel.Notifications;
using Valt.UI.Base;

namespace Valt.Tests.Architecture;

[TestFixture]
public class NamingConventionTests
{
    private static readonly Assembly CoreAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly AppAssembly = typeof(Valt.App.Extensions.AssemblyMarker).Assembly;
    private static readonly Assembly InfraAssembly = typeof(Valt.Infra.Extensions.Foo).Assembly;
    private static readonly Assembly UIAssembly = typeof(Valt.UI.App).Assembly;

    #region ViewModel Naming

    [Test]
    public void ViewModels_Should_End_With_ViewModel()
    {
        var result = Types.InAssembly(UIAssembly)
            .That()
            .Inherit(typeof(ValtViewModel))
            .Should()
            .HaveNameEndingWith("ViewModel")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"ViewModels should end with 'ViewModel'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Page_ViewModels_Should_Inherit_ValtViewModel()
    {
        // Page/Tab/Modal ViewModels should inherit from ValtViewModel
        // Item ViewModels (e.g., TransactionViewModel, AccountViewModel) are simple data classes
        // and don't need to inherit from ValtViewModel
        var pageViewModelPatterns = new[]
        {
            "TabViewModel",
            "PageViewModel",
            "ModalViewModel",
            "EditorViewModel",
            "DialogViewModel"
        };

        var result = Types.InAssembly(UIAssembly)
            .That()
            .Inherit(typeof(ValtViewModel))
            .Should()
            .HaveNameEndingWith("ViewModel")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Classes inheriting from ValtViewModel should end with 'ViewModel'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Query Naming

    [Test]
    public void Queries_Should_End_With_Query()
    {
        var result = Types.InAssembly(AppAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Queries should end with 'Query'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void QueryHandlers_Should_End_With_Handler()
    {
        var result = Types.InAssembly(AppAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Query handlers should end with 'Handler'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void InfraQueryHandlers_Should_End_With_Handler()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Infra query handlers should end with 'Handler'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Command Naming

    [Test]
    public void Commands_Should_End_With_Command()
    {
        var result = Types.InAssembly(AppAssembly)
            .That()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Commands should end with 'Command'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void CommandHandlers_Should_End_With_Handler()
    {
        var result = Types.InAssembly(AppAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Command handlers should end with 'Handler'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Domain Event Naming

    [Test]
    public void DomainEventHandlers_Should_End_With_Handler()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEventHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Domain event handlers should end with 'Handler'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Notification Naming

    [Test]
    public void NotificationHandlers_Should_End_With_Handler()
    {
        var infraResult = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(INotificationHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        var uiResult = Types.InAssembly(UIAssembly)
            .That()
            .ImplementInterface(typeof(INotificationHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        var failingTypes = (infraResult.FailingTypeNames ?? [])
            .Concat(uiResult.FailingTypeNames ?? [])
            .ToList();

        Assert.That(infraResult.IsSuccessful && uiResult.IsSuccessful,
            () => $"Notification handlers should end with 'Handler'. Violating types: {string.Join(", ", failingTypes)}");
    }

    #endregion

    #region Repository Naming

    [Test]
    public void Repositories_Should_End_With_Repository()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(Valt.Core.Kernel.Abstractions.IRepository))
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Repositories should end with 'Repository'. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion
}
