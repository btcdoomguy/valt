namespace Valt.App.Modules.SpendingAnalytics.DTOs;

public record SavingsRateDataDto
{
    public required IReadOnlyList<SavingsRateMonthDto> Months { get; init; }
    public required string PrimaryCurrency { get; init; }
}

public record SavingsRateMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal? Rate { get; init; }
}
