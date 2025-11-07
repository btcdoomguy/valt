namespace Valt.Core.Kernel.Abstractions.EventSystem;

public interface IDomainEventPublisher
{
    Task PublishAsync<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent;
}