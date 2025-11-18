using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.Core.Modules.Budget.Transactions;

public record TransactionFixedExpenseReference(FixedExpenseId FixedExpenseId, DateOnly ReferenceDate);