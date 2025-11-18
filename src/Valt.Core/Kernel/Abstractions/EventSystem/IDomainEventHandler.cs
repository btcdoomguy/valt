namespace Valt.Core.Kernel.Abstractions.EventSystem;

public interface IDomainEventHandler<in TEvent> where TEvent : class, IDomainEvent
{
    Task HandleAsync(TEvent @event);
}