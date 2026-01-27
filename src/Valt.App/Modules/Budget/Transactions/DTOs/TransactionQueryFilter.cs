namespace Valt.App.Modules.Budget.Transactions.DTOs;

public record TransactionQueryFilter
{
    public string[]? AccountIds { get; init; }
    public string[]? CategoryIds { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? SearchTerm { get; init; }
}
