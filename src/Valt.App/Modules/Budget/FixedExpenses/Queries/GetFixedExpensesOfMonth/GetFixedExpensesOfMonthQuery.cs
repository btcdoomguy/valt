using Valt.App.Kernel.Queries;
using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpensesOfMonth;

public sealed record GetFixedExpensesOfMonthQuery(DateOnly MonthYear) : IQuery<IReadOnlyList<FixedExpenseEntryDTO>>;

public record FixedExpenseEntryDTO
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

    public string? TransactionId { get; protected set; }
    public decimal? ActualAmount { get; protected set; }
    public string? ActualCurrency { get; protected set; }
    public FixedExpenseRecordState State { get; protected set; }

    public bool Paid => State == FixedExpenseRecordState.Paid && TransactionId != null;
    public bool Ignored => State == FixedExpenseRecordState.Ignored;
    public bool MarkedAsPaid => State == FixedExpenseRecordState.ManuallyPaid;
    public bool Empty => State == FixedExpenseRecordState.Empty;

    public int Day => ReferenceDate.Day;

    public FixedExpenseEntryDTO(
        string id,
        string name,
        string? categoryId,
        DateOnly referenceDate,
        string? defaultAccountId,
        decimal? fixedAmount,
        decimal? rangedAmountMin,
        decimal? rangedAmountMax,
        string currency)
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

    public FixedExpenseEntryDTO(
        string id,
        string name,
        string? categoryId,
        DateOnly referenceDate,
        string? defaultAccountId,
        decimal? fixedAmount,
        decimal? rangedAmountMin,
        decimal? rangedAmountMax,
        string currency,
        FixedExpenseRecordState state,
        string? transactionId)
        : this(id, name, categoryId, referenceDate, defaultAccountId, fixedAmount, rangedAmountMin, rangedAmountMax, currency)
    {
        State = state;
        TransactionId = transactionId;
    }

    public void Pay(string transactionId)
    {
        TransactionId = transactionId;
        State = FixedExpenseRecordState.Paid;
    }

    public void Pay(string transactionId, decimal? actualAmount)
        => Pay(transactionId, actualAmount, null);

    public void Pay(string transactionId, decimal? actualAmount, string? actualCurrency)
    {
        TransactionId = transactionId;
        ActualAmount = actualAmount;
        ActualCurrency = actualCurrency;
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
