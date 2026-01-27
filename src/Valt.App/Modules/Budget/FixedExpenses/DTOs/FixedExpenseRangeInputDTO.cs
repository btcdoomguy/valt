namespace Valt.App.Modules.Budget.FixedExpenses.DTOs;

/// <summary>
/// Input DTO for creating or editing fixed expense ranges.
/// </summary>
public record FixedExpenseRangeInputDTO
{
    public required DateOnly PeriodStart { get; init; }

    /// <summary>
    /// The fixed amount if this is a fixed-amount range. Mutually exclusive with RangedAmountMin/Max.
    /// </summary>
    public decimal? FixedAmount { get; init; }

    /// <summary>
    /// The minimum ranged amount if this is a ranged-amount range. Must be provided with RangedAmountMax.
    /// </summary>
    public decimal? RangedAmountMin { get; init; }

    /// <summary>
    /// The maximum ranged amount if this is a ranged-amount range. Must be provided with RangedAmountMin.
    /// </summary>
    public decimal? RangedAmountMax { get; init; }

    /// <summary>
    /// Period type: 0=Monthly, 1=Yearly, 2=Weekly, 3=Biweekly
    /// </summary>
    public required int PeriodId { get; init; }

    /// <summary>
    /// Day of month (1-31) for Monthly/Yearly periods, or day of week (0-6, Sunday=0) for Weekly/Biweekly.
    /// </summary>
    public required int Day { get; init; }
}
