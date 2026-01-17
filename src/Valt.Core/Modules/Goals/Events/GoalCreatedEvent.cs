using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Goals.Events;

public sealed record GoalCreatedEvent(Goal Goal) : IDomainEvent;
