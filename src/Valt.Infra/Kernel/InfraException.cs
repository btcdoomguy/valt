namespace Valt.Infra.Kernel;

public abstract class InfraException : Exception
{
    public virtual string? Code { get; }

    protected InfraException(string message) : base(message)
    {
    }

    protected InfraException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    protected InfraException(string code, string message) : base(message)
    {
        Code = code;
    }

    protected InfraException(string code, string message, Exception? innerException) : base(message, innerException)
    {
        Code = code;
    }
}