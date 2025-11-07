namespace Valt.Infra.Kernel.Scopes;

public class ContextScope : IContextScope
{
    private IServiceProvider? _currentServiceProvider;

    public IServiceProvider GetCurrentServiceProvider()
    {
        if (_currentServiceProvider is not null)
            return _currentServiceProvider;

        throw new NoScopeAvailableException();
    }

    public void SetCustomServiceProvider(IServiceProvider serviceProvider)
    {
        _currentServiceProvider = serviceProvider;
    }
}