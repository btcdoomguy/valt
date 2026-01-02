using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.AllTimeHigh;

public record AllTimeHighData(DateOnly Date, FiatCurrency Currency, FiatValue Value, decimal DeclineFromAth)
{
    public bool HasAccountsWithoutTransactions { get; init; }

    /// <summary>
    /// The date when the maximum drawdown from ATH occurred (lowest point after ATH)
    /// </summary>
    public DateOnly? MaxDrawdownDate { get; init; }

    /// <summary>
    /// The maximum drawdown percentage from ATH (e.g., -50 means dropped 50% from ATH)
    /// </summary>
    public decimal? MaxDrawdownPercent { get; init; }
}