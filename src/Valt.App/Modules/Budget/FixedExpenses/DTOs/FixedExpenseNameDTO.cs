namespace Valt.App.Modules.Budget.FixedExpenses.DTOs;

public record FixedExpenseNameDTO(string Id, string Name)
{
    public static FixedExpenseNameDTO Empty => new(string.Empty, string.Empty);
}

public record FixedExpenseNamesDTO(IReadOnlyList<FixedExpenseNameDTO> Items);
