namespace Valt.App.Modules.BtcDenominatedMetrics.DTOs;

public record BtcDenominatedMetricsDataDto
{
    public required IReadOnlyList<BtcDenominatedMetricsMonthDto> Months { get; init; }
    public required string PrimaryCurrency { get; init; }
}

public record BtcDenominatedMetricsMonthDto
{
    public required DateOnly Month { get; init; }
    public required long SatsEarned { get; init; }
    public required long SatsSpent { get; init; }
    public required long StackVelocity { get; init; }
}
