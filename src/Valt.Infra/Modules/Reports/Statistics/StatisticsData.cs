using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.Statistics;

/// <summary>
/// Represents statistics data including median expenses and wealth coverage
/// </summary>
public record StatisticsData
{
    /// <summary>
    /// The median monthly expense value from the last 12 months (as positive value)
    /// </summary>
    public required FiatValue MedianMonthlyExpenses { get; init; }

    /// <summary>
    /// The currency used for calculations
    /// </summary>
    public required FiatCurrency Currency { get; init; }

    /// <summary>
    /// Number of months the current wealth can cover based on median expenses
    /// </summary>
    public required int WealthCoverageMonths { get; init; }

    /// <summary>
    /// Formatted string for wealth coverage (e.g., "1 year 3 months" or "8 months")
    /// </summary>
    public required string WealthCoverageFormatted { get; init; }

    /// <summary>
    /// Whether previous period median expenses data is available
    /// </summary>
    public required bool HasMedianMonthlyExpensesPreviousPeriod { get; init; }

    /// <summary>
    /// The median monthly expense value from the previous 12 months period (as positive value)
    /// </summary>
    public FiatValue? MedianMonthlyExpensesPreviousPeriod { get; init; }

    /// <summary>
    /// The percentage evolution between current and previous period medians.
    /// Positive value means expenses increased, negative means decreased.
    /// </summary>
    public decimal? MedianMonthlyExpensesEvolution { get; init; }

    /// <summary>
    /// Whether sat-based median data is available (has transactions with AutoSatAmount calculated)
    /// </summary>
    public required bool HasMedianMonthlyExpensesSats { get; init; }

    /// <summary>
    /// The median monthly expense value in satoshis from the last 12 months (as positive value)
    /// </summary>
    public long? MedianMonthlyExpensesSats { get; init; }

    /// <summary>
    /// The median monthly expense value in satoshis from the previous 12 months period (as positive value)
    /// </summary>
    public long? MedianMonthlyExpensesPreviousPeriodSats { get; init; }

    /// <summary>
    /// The percentage evolution between current and previous period sat medians.
    /// Positive value means expenses increased, negative means decreased.
    /// </summary>
    public decimal? MedianMonthlyExpensesSatsEvolution { get; init; }
}
