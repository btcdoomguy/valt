using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses;

namespace Valt.Tests.Builders;

public class FixedExpenseBuilder
{
    public FixedExpenseId Id { get; set; } = new();
    public FixedExpenseName Name { get; set; } = null!;
    public AccountId? DefaultAccountId { get; set; }
    public CategoryId CategoryId { get; set; }
    public FiatCurrency? Currency { get; set; }
    public List<FixedExpenseRange> Ranges { get; set; } = new();
    public DateOnly? LastFixedExpenseRecordDate { get; set; }
    public bool Enabled { get; set; } = true;
    public int Version { get; set; } = 0;

    public FixedExpenseEntity Build()
    {
        return FixedExpense.Create(Id, Name, DefaultAccountId, CategoryId, Currency, Ranges, LastFixedExpenseRecordDate, Enabled, Version).AsEntity();
    }
}