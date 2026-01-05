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
}
