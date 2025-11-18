using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

public record TransactionQueryFilter
{
    public AccountId[]? Accounts { get; set; }
    public CategoryId[]? Categories { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string? SearchTerm { get; set; }
}