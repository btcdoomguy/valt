using System.Reflection;
using NetArchTest.Rules;
using Valt.Core.Kernel;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Tests.Architecture;

/// <summary>
/// Tests that enforce the layered architecture:
/// Core (domain) -> App (application) -> Infra (infrastructure) -> UI (presentation)
///
/// Dependencies should only flow upward (UI can reference all, Infra can reference App/Core, etc.)
/// </summary>
[TestFixture]
public class LayerDependencyTests
{
    private static readonly Assembly CoreAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly AppAssembly = typeof(Valt.App.Extensions.AssemblyMarker).Assembly;
    private static readonly Assembly InfraAssembly = typeof(Valt.Infra.Extensions.Foo).Assembly;
    private static readonly Assembly UIAssembly = typeof(Valt.UI.App).Assembly;

    #region Core Layer Dependencies (innermost - no dependencies on other Valt layers)

    [Test]
    public void Core_Should_Not_Reference_App_Layer()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Valt.App")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Core layer should not reference App layer. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Core_Should_Not_Reference_Infra_Layer()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Valt.Infra")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Core layer should not reference Infra layer. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Core_Should_Not_Reference_UI_Layer()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Valt.UI")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Core layer should not reference UI layer. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Core_Should_Not_Reference_LiteDB()
    {
        // Domain layer should be persistence-ignorant
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("LiteDB")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Core layer should not reference LiteDB (persistence ignorance). Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Core_Should_Not_Reference_Avalonia()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Avalonia")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Core layer should not reference Avalonia. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Core_Should_Not_Reference_Newtonsoft_Json()
    {
        // Domain should not have serialization concerns
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Newtonsoft.Json")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Core layer should not reference Newtonsoft.Json. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Core_Should_Not_Reference_System_Text_Json_Serialization()
    {
        // Domain entities should not have JSON attributes
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.Text.Json.Serialization")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Core layer should not have JSON serialization attributes. Use DTOs in Infra layer. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region App Layer Dependencies (can reference Core only)

    [Test]
    public void App_Should_Not_Reference_Infra_Layer()
    {
        var result = Types.InAssembly(AppAssembly)
            .ShouldNot()
            .HaveDependencyOn("Valt.Infra")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"App layer should not reference Infra layer. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void App_Should_Not_Reference_UI_Layer()
    {
        var result = Types.InAssembly(AppAssembly)
            .ShouldNot()
            .HaveDependencyOn("Valt.UI")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"App layer should not reference UI layer. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void App_Should_Not_Reference_Avalonia()
    {
        var result = Types.InAssembly(AppAssembly)
            .ShouldNot()
            .HaveDependencyOn("Avalonia")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"App layer should not reference Avalonia. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void App_Should_Not_Reference_LiteDB()
    {
        // App layer should be persistence-ignorant (repositories are in Infra)
        var result = Types.InAssembly(AppAssembly)
            .ShouldNot()
            .HaveDependencyOn("LiteDB")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"App layer should not reference LiteDB. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Infra Layer Dependencies (can reference Core and App)

    [Test]
    public void Infra_Should_Not_Reference_UI_Layer()
    {
        var result = Types.InAssembly(InfraAssembly)
            .ShouldNot()
            .HaveDependencyOn("Valt.UI")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Infra layer should not reference UI layer. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Infra_Should_Not_Reference_Avalonia()
    {
        var result = Types.InAssembly(InfraAssembly)
            .ShouldNot()
            .HaveDependencyOn("Avalonia")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Infra layer should not reference Avalonia. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Test]
    public void Infra_Should_Not_Reference_WeakReferenceMessenger()
    {
        // WeakReferenceMessenger is a UI-layer concern (CommunityToolkit.Mvvm.Messaging)
        // Infra should use INotificationPublisher instead
        var result = Types.InAssembly(InfraAssembly)
            .ShouldNot()
            .HaveDependencyOn("CommunityToolkit.Mvvm.Messaging")
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Infra layer should not reference WeakReferenceMessenger. " +
                  $"Use INotificationPublisher instead. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Notification Rules

    [Test]
    public void Notifications_Should_Not_Be_Abstract()
    {
        // Notifications should be concrete types (records or sealed classes)
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ImplementInterface(typeof(INotification))
            .ShouldNot()
            .BeAbstract()
            .GetResult();

        Assert.That(result.IsSuccessful,
            () => $"Notifications should be concrete types. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    #endregion
}
