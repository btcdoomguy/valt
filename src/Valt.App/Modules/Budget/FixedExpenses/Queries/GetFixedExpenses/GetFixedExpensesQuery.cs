using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenses;

public record GetFixedExpensesQuery : IQuery<IReadOnlyList<FixedExpenseDTO>>;
