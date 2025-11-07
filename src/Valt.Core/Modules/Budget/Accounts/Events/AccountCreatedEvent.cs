using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Budget.Accounts.Events;

public sealed record AccountCreatedEvent(Account Account) : IDomainEvent;