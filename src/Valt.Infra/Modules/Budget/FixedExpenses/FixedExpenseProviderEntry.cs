using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

public record FixedExpenseProviderEntry
{
    public string Id { get; }
    public string Name { get; }
    public string? CategoryId { get; }
    public DateOnly ReferenceDate { get; }
    public string? DefaultAccountId { get; }
    public decimal? FixedAmount { get; }
    public decimal? RangedAmountMin { get; }
    public decimal? RangedAmountMax { get; }
    public string Currency { get; }
    
    public decimal MinimumAmount => FixedAmount ?? RangedAmountMin ?? 0;
    public decimal MaximumAmount => FixedAmount ?? RangedAmountMax ?? 0;

    public FixedExpenseProviderEntry(string id, string name, string? categoryId, DateOnly referenceDate,
        string? defaultAccountId, decimal? fixedAmount, decimal? rangedAmountMin, decimal? rangedAmountMax, string currency)
    {
        Id = id;
        Name = name;
        CategoryId = categoryId;
        ReferenceDate = referenceDate;
        DefaultAccountId = defaultAccountId;
        FixedAmount = fixedAmount;
        RangedAmountMin = rangedAmountMin;
        RangedAmountMax = rangedAmountMax;
        Currency = currency;
    }

    public FixedExpenseProviderEntry(string id, string name, string? categoryId, DateOnly referenceDate,
        string? defaultAccountId, decimal? fixedAmount, decimal? rangedAmountMin, decimal? rangedAmountMax, string currency,
        FixedExpenseRecordState state, string? transactionId) : this(id,
        name, categoryId, referenceDate, defaultAccountId, fixedAmount, rangedAmountMin, rangedAmountMax, currency)
    {
        State = state;
        TransactionId = transactionId;
    }

    public string? TransactionId { get; private set; }
    public FixedExpenseRecordState State { get; private set; }

    public bool Paid => State == FixedExpenseRecordState.Paid && TransactionId != null;
    public bool Ignored => State == FixedExpenseRecordState.Ignored;
    public bool MarkedAsPaid => State == FixedExpenseRecordState.ManuallyPaid;
    public bool Empty => State == FixedExpenseRecordState.Empty;

    public int Day => ReferenceDate.Day;

    public void Pay(string transactionId)
    {
        TransactionId = transactionId;
        State = FixedExpenseRecordState.Paid;
    }

    public void Ignore()
    {
        TransactionId = null;
        State = FixedExpenseRecordState.Ignored;
    }

    public void MarkAsPaid()
    {
        TransactionId = null;
        State = FixedExpenseRecordState.ManuallyPaid;
    }
}