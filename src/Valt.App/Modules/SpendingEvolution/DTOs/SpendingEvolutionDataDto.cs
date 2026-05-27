namespace Valt.App.Modules.SpendingEvolution.DTOs;

public record SpendingEvolutionDataDto
{
    public required IReadOnlyList<SpendingEvolutionMonthDto> Months { get; init; }
    public required bool HasMissingPriceInSats { get; init; }
    public required string PrimaryCurrency { get; init; }
}
