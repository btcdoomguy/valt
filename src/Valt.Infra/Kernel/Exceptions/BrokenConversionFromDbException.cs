namespace Valt.Infra.Kernel.Exceptions;

public class BrokenConversionFromDbException : InfraException
{
    public BrokenConversionFromDbException(string entityName, string entityId, Exception? innerException) : base(
        $"Could not read properly from DB the entity {entityName} with ID {entityId}", innerException)
    {
    }
}