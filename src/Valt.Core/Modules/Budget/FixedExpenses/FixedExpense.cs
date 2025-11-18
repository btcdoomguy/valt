using Valt.Core.Common;
using Valt.Core.Kernel;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses.Events;
using Valt.Core.Modules.Budget.FixedExpenses.Exceptions;

namespace Valt.Core.Modules.Budget.FixedExpenses;

public sealed class FixedExpense : AggregateRoot<FixedExpenseId>
{
    private readonly ISet<FixedExpenseRange> _ranges;
    public FixedExpenseName Name { get; private set; } = null!;
    public AccountId? DefaultAccountId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public FiatCurrency? Currency { get; private set; }
    public DateOnly? LastFixedExpenseRecordDate { get; private set; }
    public bool Enabled { get; private set; }
    public IEnumerable<FixedExpenseRange> Ranges => _ranges;
    public FixedExpenseRange CurrentRange => Ranges.Last();

    private FixedExpense(FixedExpenseId id, FixedExpenseName name, AccountId? defaultAccountId, CategoryId categoryId,
        FiatCurrency? currency,
        IReadOnlyCollection<FixedExpenseRange> ranges, DateOnly? lastFixedExpenseRecordDate, bool enabled, int version)
    {
        if (ranges.Count == 0)
            throw new ArgumentException("At least one range must be provided", nameof(ranges));

        if (currency is null && defaultAccountId is null || currency is not null && defaultAccountId is not null)
            throw new ArgumentException("Either default account or currency must be provided", nameof(currency));

        Id = id;
        Name = name;
        DefaultAccountId = defaultAccountId;
        CategoryId = categoryId;
        Currency = currency;
        _ranges = new HashSet<FixedExpenseRange>(ranges.OrderBy(r => r.PeriodStart));
        Enabled = enabled;
        LastFixedExpenseRecordDate = lastFixedExpenseRecordDate;
        Version = version;

        if (Version == 0)
            AddEvent(new FixedExpenseCreatedEvent(this));
    }

    public static FixedExpense New(FixedExpenseName name, AccountId? defaultAccountId, CategoryId categoryId,
        FiatCurrency? currency,
        IReadOnlyCollection<FixedExpenseRange> ranges, bool enabled)
    {
        return new FixedExpense(new FixedExpenseId(), name, defaultAccountId, categoryId, currency, ranges, null, enabled, 0);
    }

    public static FixedExpense Create(FixedExpenseId id, FixedExpenseName name, AccountId? defaultAccountId,
        CategoryId categoryId, FiatCurrency? currency,
        IReadOnlyCollection<FixedExpenseRange> ranges, DateOnly? lastFixedExpenseRecordDate, bool enabled,
        int version)
    {
        return new FixedExpense(id, name, defaultAccountId, categoryId, currency, ranges.OrderBy(x => x.PeriodStart).ToList(), lastFixedExpenseRecordDate, enabled, version);
    }

    public void Rename(FixedExpenseName name)
    {
        if (Name == name)
            return;
        
        Name = name;

        AddEvent(new FixedExpenseEditedEvent(this));
    }

    public void SetDefaultAccountId(AccountId? defaultAccountId)
    {
        if (DefaultAccountId == defaultAccountId)
            return;
        
        DefaultAccountId = defaultAccountId;
        Currency = null;

        AddEvent(new FixedExpenseEditedEvent(this));
    }

    public void SetCurrency(FiatCurrency? fiatCurrency)
    {
        if (Currency == fiatCurrency)
            return;
        
        Currency = fiatCurrency;
        DefaultAccountId = null;

        AddEvent(new FixedExpenseEditedEvent(this));
    }

    public void AddRange(FixedExpenseRange range)
    {
        if (LastFixedExpenseRecordDate.HasValue && range.PeriodStart <= LastFixedExpenseRecordDate)
            throw new InvalidFixedExpenseRangeException(LastFixedExpenseRecordDate.Value);
        
        var rangesToRemove = _ranges.Where(r => r.PeriodStart > LastFixedExpenseRecordDate).ToList();
        foreach (var rangeToRemove in rangesToRemove)
            _ranges.Remove(rangeToRemove);
        
        _ranges.Add(range);
        
        AddEvent(new FixedExpenseEditedEvent(this));
    }
    
    public bool ContainsRange(FixedExpenseRange range)
    {
        return _ranges.Contains(range);
    }

    public void SetEnabled(bool enabled)
    {
        if (Enabled == enabled)
            return;
        
        Enabled = enabled;

        AddEvent(new FixedExpenseEditedEvent(this));
    }

    public void SetCategory(CategoryId categoryId)
    {
        if (CategoryId == categoryId)
            return;
        
        CategoryId = categoryId;

        AddEvent(new FixedExpenseEditedEvent(this));
    }
}