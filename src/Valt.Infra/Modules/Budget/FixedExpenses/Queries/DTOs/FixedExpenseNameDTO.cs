namespace Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

public record FixedExpenseNameDTO(string Id, string Name)
{
    public static FixedExpenseNameDTO Empty => new(string.Empty, string.Empty);
}