namespace Valt.App.Modules.Budget.Transactions.DTOs;

public record TransactionDTO
{
    public string Id { get; init; } = null!;
    public DateOnly Date { get; init; }
    public string Name { get; init; } = null!;
    public string CategoryId { get; init; } = null!;
    public string CategoryName { get; init; } = null!;
    public string? CategoryIcon { get; init; }
    public string FromAccountId { get; init; } = null!;
    public string FromAccountName { get; init; } = null!;
    public string? FromAccountIcon { get; init; } = null!;
    public string? FromAccountCurrency { get; init; } = null!;
    public string? ToAccountId { get; init; }
    public string? ToAccountName { get; init; }
    public string? ToAccountIcon { get; init; }
    public string? ToAccountCurrency { get; init; }
    public string? FormattedFromAmount { get; set; }
    public long? FromAmountSats { get; set; }
    public decimal? FromAmountFiat { get; set; }
    public string? FormattedToAmount { get; set; }
    public long? ToAmountSats { get; set; }
    public decimal? ToAmountFiat { get; set; }
    public string TransferType { get; set; } = null!;
    public string TransactionType { get; set; } = null!;
    public long? AutoSatAmount { get; set; }
    public string AutoSatAmountSummary { get; set; } = null!;

    public string? FixedExpenseRecordId { get; set; }
    public string? FixedExpenseId { get; set; }
    public string? FixedExpenseName { get; set; }
    public DateOnly? FixedExpenseReferenceDate { get; set; }
    public string? Notes { get; set; }
}
