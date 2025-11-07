namespace Valt.Infra.Kernel.Scopes;

public class NoScopeAvailableException : InfraException
{
    public NoScopeAvailableException() : base("There is no scope available for this operation")
    {
    }
}