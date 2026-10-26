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
    public required Totals Total { get; init; }
    
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
        
        public required decimal AllIncomeInFiat { get; init; }
        public required decimal AllExpensesInFiat { get; init; }

        /// <summary>
        /// Aggregate "Included in Wealth Totals" asset value from the latest wealth snapshot
        /// on or before this period's end date, in the report's main currency. Zero when no
        /// snapshot exists on or before the period end (or the snapshot currency mismatches).
        /// </summary>
        public decimal AssetsTotalFiat { get; init; }

        /// <summary>
        /// Same snapshot value expressed in satoshis; zero under the same fallback conditions.
        /// </summary>
        public long AssetsTotalSats { get; init; }
    }

    public record Totals
    {
        public required decimal Income { get; init; }
        public required decimal Expenses { get; init; }
        
        public required decimal BitcoinPurchased { get; init; }
        public required decimal BitcoinSold { get; init; }
        
        public required decimal BitcoinIncome { get; init; }
        public required decimal BitcoinExpenses { get; init; }
        
        public required decimal AllIncomeInFiat { get; init; }
        public required decimal AllExpensesInFiat { get; init; }
    }
}