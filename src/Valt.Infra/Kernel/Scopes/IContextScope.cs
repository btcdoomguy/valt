namespace Valt.Infra.Kernel.Scopes;

/// <summary>
/// Provides a scope inside the current request or background service
/// </summary>
public interface IContextScope
{
    IServiceProvider GetCurrentServiceProvider();
    void SetCustomServiceProvider(IServiceProvider serviceProvider);
}