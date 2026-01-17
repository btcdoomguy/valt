using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Goals.Events;

public sealed record GoalDeletedEvent(Goal Goal) : IDomainEvent;
