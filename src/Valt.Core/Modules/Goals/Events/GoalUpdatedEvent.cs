using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Goals.Events;

public sealed record GoalUpdatedEvent(Goal Goal) : IDomainEvent;
