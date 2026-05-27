namespace Valt.App.Modules.SpendingEvolution.DTOs;

public record SpendingEvolutionMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal FiatTotal { get; init; }
    public required long SatsTotal { get; init; }
    public required int TransactionCount { get; init; }
}
