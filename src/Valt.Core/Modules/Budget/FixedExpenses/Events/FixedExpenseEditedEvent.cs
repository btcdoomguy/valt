using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Budget.FixedExpenses.Events;

public sealed record FixedExpenseEditedEvent(FixedExpense FixedExpense) : IDomainEvent;