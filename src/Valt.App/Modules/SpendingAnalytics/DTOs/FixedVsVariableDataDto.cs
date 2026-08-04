namespace Valt.App.Modules.SpendingAnalytics.DTOs;

public record FixedVsVariableDataDto
{
    public required IReadOnlyList<FixedVsVariableMonthDto> Months { get; init; }
    public required bool HasNoFixedExpenses { get; init; }
    public required string PrimaryCurrency { get; init; }
}

public record FixedVsVariableMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal FixedTotal { get; init; }
    public required decimal VariableTotal { get; init; }
}
