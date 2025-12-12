using Valt.Core.Common;
using Valt.Infra.Kernel;

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
        
        public required decimal Income { get; init; }
        public required decimal Expenses { get; init; }
        
        public required decimal BitcoinPurchased { get; init; }
        public required decimal BitcoinSold { get; init; }
        
        public required decimal BitcoinIncome { get; init; }
        public required decimal BitcoinExpenses { get; init; }
    }
}