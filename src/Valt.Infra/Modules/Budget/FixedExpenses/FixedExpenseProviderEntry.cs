using Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpensesOfMonth;
using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

public record FixedExpenseProviderEntry : FixedExpenseEntryDTO
{
    public FixedExpenseProviderEntry(string id, string name, string? categoryId, DateOnly referenceDate,
        string? defaultAccountId, decimal? fixedAmount, decimal? rangedAmountMin, decimal? rangedAmountMax, string currency)
        : base(id, name, categoryId, referenceDate, defaultAccountId, fixedAmount, rangedAmountMin, rangedAmountMax, currency)
    {
    }

    public FixedExpenseProviderEntry(string id, string name, string? categoryId, DateOnly referenceDate,
        string? defaultAccountId, decimal? fixedAmount, decimal? rangedAmountMin, decimal? rangedAmountMax, string currency,
        FixedExpenseRecordState state, string? transactionId)
        : base(id, name, categoryId, referenceDate, defaultAccountId, fixedAmount, rangedAmountMin, rangedAmountMax, currency, state, transactionId)
    {
    }
}
