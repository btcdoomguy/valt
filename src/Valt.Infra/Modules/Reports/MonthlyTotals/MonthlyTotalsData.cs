using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.MonthlyTotals;


/// <summary>
/// Represents the monthly totals data
/// </summary>
public record MonthlyTotalsData
{
    public required FiatCurrency MainCurrency { get; init; }
    public required IReadOnlyList<Item> Items { get; init; }
    
    public record Item
    {
        public required DateOnly MonthYear { get; init; }
        public required decimal BtcTotal { get; init; }
        public required decimal BtcMonthlyChange { get; init; }
        public required decimal BtcYearlyChange { get; init; }

        public required decimal FiatTotal { get; init; }
        public required decimal FiatMonthlyChange { get; init; }
        public required decimal FiatYearlyChange { get; init; }
    }
}