namespace Valt.Core.Kernel.Exceptions;

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, string entityId) : base(
        $"Entity {entityName} ID {entityId} not found")
    {
    }
}