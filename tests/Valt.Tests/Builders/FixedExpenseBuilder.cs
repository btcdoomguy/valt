using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses;

namespace Valt.Tests.Builders;

/// <summary>
/// Builder for creating FixedExpense test data.
/// FixedExpense represents a recurring expense with fixed or ranged amounts.
/// Supports both fluent API (preferred) and property initialization syntax.
/// </summary>
public class FixedExpenseBuilder
{
    private FixedExpenseId _id = new();
    private FixedExpenseName _name = "Test Fixed Expense";
    private AccountId? _defaultAccountId;
    private CategoryId _categoryId = new();
    private FiatCurrency? _currency;
    private List<FixedExpenseRange> _ranges = new();
    private DateOnly? _lastFixedExpenseRecordDate;
    private bool _enabled = true;
    private int _version = 0;

    // Public properties for backward compatibility with property initializer syntax
    public FixedExpenseId Id { get => _id; set => _id = value; }
    public FixedExpenseName Name { get => _name; set => _name = value; }
    public AccountId? DefaultAccountId { get => _defaultAccountId; set => _defaultAccountId = value; }
    public CategoryId CategoryId { get => _categoryId; set => _categoryId = value; }
    public FiatCurrency? Currency { get => _currency; set => _currency = value; }
    public List<FixedExpenseRange> Ranges { get => _ranges; set => _ranges = value; }
    public DateOnly? LastFixedExpenseRecordDate { get => _lastFixedExpenseRecordDate; set => _lastFixedExpenseRecordDate = value; }
    public bool Enabled { get => _enabled; set => _enabled = value; }
    public int Version { get => _version; set => _version = value; }

    public static FixedExpenseBuilder AFixedExpense() => new();

    /// <summary>
    /// Creates a fixed expense with an account (uses account's currency).
    /// </summary>
    public static FixedExpenseBuilder AFixedExpenseWithAccount(AccountId accountId) =>
        new FixedExpenseBuilder().WithDefaultAccountId(accountId);

    /// <summary>
    /// Creates a fixed expense with a currency (no default account).
    /// </summary>
    public static FixedExpenseBuilder AFixedExpenseWithCurrency(FiatCurrency currency) =>
        new FixedExpenseBuilder().WithCurrency(currency);

    public FixedExpenseBuilder WithId(FixedExpenseId id)
    {
        _id = id;
        return this;
    }

    public FixedExpenseBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public FixedExpenseBuilder WithDefaultAccountId(AccountId? accountId)
    {
        _defaultAccountId = accountId;
        return this;
    }

    public FixedExpenseBuilder WithCategoryId(CategoryId categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public FixedExpenseBuilder WithCurrency(FiatCurrency? currency)
    {
        _currency = currency;
        return this;
    }

    public FixedExpenseBuilder WithRanges(params FixedExpenseRange[] ranges)
    {
        _ranges = ranges.ToList();
        return this;
    }

    public FixedExpenseBuilder WithFixedAmountRange(decimal amount, FixedExpensePeriods period, DateOnly startDate, int dayOfPeriod)
    {
        _ranges.Add(FixedExpenseRange.CreateFixedAmount(
            FiatValue.New(amount), period, startDate, dayOfPeriod));
        return this;
    }

    public FixedExpenseBuilder WithRangedAmountRange(decimal minAmount, decimal maxAmount, FixedExpensePeriods period, DateOnly startDate, int dayOfPeriod)
    {
        _ranges.Add(FixedExpenseRange.CreateRangedAmount(
            new RangedFiatValue(FiatValue.New(minAmount), FiatValue.New(maxAmount)),
            period, startDate, dayOfPeriod));
        return this;
    }

    public FixedExpenseBuilder WithLastFixedExpenseRecordDate(DateOnly? date)
    {
        _lastFixedExpenseRecordDate = date;
        return this;
    }

    public FixedExpenseBuilder WithEnabled(bool enabled)
    {
        _enabled = enabled;
        return this;
    }

    public FixedExpenseBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public FixedExpense BuildDomainObject()
    {
        return FixedExpense.Create(_id, _name, _defaultAccountId, _categoryId, _currency, _ranges, _lastFixedExpenseRecordDate, _enabled, _version);
    }

    public FixedExpenseEntity Build()
    {
        return BuildDomainObject().AsEntity();
    }
}
