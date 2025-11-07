using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Budget.Accounts.Events;

public sealed record AccountInitialAmountChangedEvent(
    Account Account,
    BtcValue? PreviousBtcInitialAmount,
    FiatValue? PreviousFiatInitialAmount) : IDomainEvent;