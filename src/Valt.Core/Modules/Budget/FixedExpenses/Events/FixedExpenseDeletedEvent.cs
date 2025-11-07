using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Budget.FixedExpenses.Events;

public sealed record FixedExpenseDeletedEvent(FixedExpense FixedExpense) : IDomainEvent;