using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Budget.Transactions.Events;

public sealed record TransactionEditedEvent(Transaction Transaction) : IDomainEvent;