namespace Valt.Infra.Kernel.Exceptions;

public class BrokenConversionToDbException : InfraException
{
    public BrokenConversionToDbException(string domainEntityName, string? domainEntityId, Exception? innerException) :
        base(
            $"Could not convert properly the domain object {domainEntityName} with ID {domainEntityId} to the DB representation",
            innerException)
    {
    }
}