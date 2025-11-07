using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Transactions.Details;

namespace Valt.Core.Modules.Budget.Transactions.Events;

public sealed record TransactionDetailsChangedEvent(Transaction Transaction, TransactionDetails PreviousDetails)
    : IDomainEvent;